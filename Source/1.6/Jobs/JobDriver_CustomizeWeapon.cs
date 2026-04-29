using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace UniqueWeaponsUnbound
{
    public class JobDriver_CustomizeWeapon : JobDriver
    {
        private const TargetIndex IngredientIndex = TargetIndex.A;
        private const TargetIndex WeaponIndex = TargetIndex.B;
        private const TargetIndex WorkbenchIndex = TargetIndex.C;
        private const int WorkTicksPerOp = 1000;

        private CustomizationSpec spec;
        private Thing weapon;
        private int currentOpIndex;
        private string phaseReport;
        private WeaponReturnMode returnMode;
        private Dictionary<ThingDef, float> refundLedger = new Dictionary<ThingDef, float>();
        private List<Thing> placedIngredients = new List<Thing>();
        // First bail reason (translated, ready to surface). Read by the finish action
        // when the job ends with Incompletable. First-set wins so downstream cascade
        // failures don't overwrite the original cause.
        private string bailMessage;

        private Building_WorkTable Workbench =>
            (Building_WorkTable)job.GetTarget(WorkbenchIndex).Thing;

        /// <summary>
        /// Best label for the weapon: the live Thing if we have one, otherwise the
        /// job target (still labelled even if despawned), otherwise a fallback.
        /// Stays valid through every bail path including pre-acquire failures.
        /// </summary>
        private string WeaponLabel
        {
            get
            {
                if (weapon != null && !weapon.Destroyed)
                    return weapon.LabelShortCap;
                Thing target = job?.GetTarget(WeaponIndex).Thing;
                if (target != null)
                    return target.LabelShortCap;
                return "UWU_WeaponFallback".Translate();
            }
        }

        /// <summary>
        /// Records the first bail reason. Subsequent calls are ignored so a primary
        /// failure isn't overwritten by a downstream cascade. The recorded text is
        /// surfaced as a top-left Messages.Message when the finish action runs.
        /// </summary>
        private void SetBailMessage(string text)
        {
            if (string.IsNullOrEmpty(bailMessage))
                bailMessage = text;
        }

        /// <summary>
        /// Records the standard "ingredients lost mid-customization" bail message
        /// for the given trait. Used by the precheck toil and by the per-op consume
        /// paths in <see cref="ApplyOperation"/>.
        /// </summary>
        private void RecordShortfallBail(WeaponTraitDef trait)
        {
            string traitLabel = trait?.LabelCap ?? "";
            SetBailMessage("UWU_IngredientShortfall".Translate(WeaponLabel, traitLabel));
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(Workbench, job, 1, -1, null, errorOnFailed))
                return false;

            // Only reserve weapon if it's spawned on the map (ground weapon case).
            // Equipped/inventory weapons don't need map reservations.
            Thing w = job.GetTarget(WeaponIndex).Thing;
            if (w != null && w.Spawned
                && !pawn.Reserve(w, job, 1, -1, null, errorOnFailed))
                return false;

            return true;
        }

        /// <summary>
        /// Finds the best cell to place an ingredient, preferring workbench cells
        /// (like vanilla's IngredientStackCells) before overflowing to nearby cells.
        /// </summary>
        private IntVec3 FindIngredientPlaceCell(Thing ingredient)
        {
            Map map = pawn.Map;
            IntVec3 interactionCell = Workbench.InteractionCell;

            // Prefer cells occupied by the workbench, closest to interaction cell first
            foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(Workbench)
                .OrderBy(c => (c - interactionCell).LengthManhattan))
            {
                if (cell == interactionCell)
                    continue;
                Thing existing = cell.GetFirstItem(map);
                if (existing == null)
                    return cell;
                if (existing.CanStackWith(ingredient)
                    && existing.stackCount < existing.def.stackLimit
                    && pawn.CanReserve(existing))
                    return cell;
            }

            // Workbench cells full — fall back to center position (Near will radiate)
            return Workbench.Position;
        }

        /// <summary>
        /// Consumes resources from the tracked placedIngredients list rather than
        /// scanning nearby cells. Mirrors vanilla's pattern of consuming from
        /// job.placedThings. Destroyed stacks are removed from the list.
        /// </summary>
        private bool ConsumeFromPlacedIngredients(List<ThingDefCountClass> costs)
        {
            foreach (ThingDefCountClass cost in costs)
            {
                int remaining = cost.count;
                for (int i = placedIngredients.Count - 1; i >= 0 && remaining > 0; i--)
                {
                    Thing stack = placedIngredients[i];
                    if (stack.Destroyed || !stack.Spawned || stack.def != cost.thingDef)
                        continue;

                    int take = Mathf.Min(remaining, stack.stackCount);
                    remaining -= take;

                    if (take >= stack.stackCount)
                    {
                        stack.Destroy();
                        placedIngredients.RemoveAt(i);
                    }
                    else
                    {
                        stack.SplitOff(take).Destroy();
                    }
                }

                if (remaining > 0)
                {
                    Log.Warning($"[Unique Weapons Unbound] Could not consume all " +
                        $"{cost.thingDef.LabelCap} from placed ingredients: " +
                        $"needed {cost.count}, short by {remaining}.");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the total reservable count of <paramref name="thingDef"/> across all
        /// currently placed ingredient stacks, ignoring destroyed/despawned ones.
        /// </summary>
        private int CountInPlaced(ThingDef thingDef)
        {
            int available = 0;
            for (int i = 0; i < placedIngredients.Count; i++)
            {
                Thing stack = placedIngredients[i];
                if (stack.Destroyed || !stack.Spawned || stack.def != thingDef)
                    continue;
                available += stack.stackCount;
            }
            return available;
        }

        /// <summary>
        /// Returns true if an op's cost could currently be paid from the refund
        /// ledger plus placed ingredients, without committing any state. Used as a
        /// pre-flight check before starting an op's work cycle so the pawn doesn't
        /// waste 1000 ticks of work on an op we already know will abort.
        /// </summary>
        private bool CanAffordOpCost(List<ThingDefCountClass> opCost)
        {
            if (opCost == null || opCost.Count == 0)
                return true;

            foreach (ThingDefCountClass cost in opCost)
            {
                int remaining = cost.count;
                if (refundLedger.TryGetValue(cost.thingDef, out float credit) && credit > 0f)
                    remaining -= Mathf.Min(remaining, Mathf.FloorToInt(credit));
                if (remaining > 0 && CountInPlaced(cost.thingDef) < remaining)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Pays an op's cost: debits the refund ledger first, then consumes the
        /// remainder from placed ingredient stacks at the workbench. Pre-checks
        /// availability and only commits if the cost can be fully paid, so a
        /// shortfall (e.g. ingredients destroyed by fire/explosion/deterioration)
        /// leaves the ledger and the weapon untouched. Returns false on shortfall —
        /// caller should notify the player and abort the job.
        /// </summary>
        private bool TryConsumeOpCost(List<ThingDefCountClass> opCost)
        {
            if (opCost == null || opCost.Count == 0)
                return true;

            // First pass: compute what we'd take from the ledger and from placed
            // ingredients, without committing.
            var fromPlaced = new List<ThingDefCountClass>();
            var pendingDebit = new Dictionary<ThingDef, int>();
            foreach (ThingDefCountClass cost in opCost)
            {
                int remaining = cost.count;
                if (refundLedger.TryGetValue(cost.thingDef, out float credit) && credit > 0f)
                {
                    int debit = Mathf.Min(remaining, Mathf.FloorToInt(credit));
                    if (debit > 0)
                    {
                        pendingDebit[cost.thingDef] = debit;
                        remaining -= debit;
                    }
                }
                if (remaining > 0)
                    fromPlaced.Add(new ThingDefCountClass(cost.thingDef, remaining));
            }

            // Verify the placed-ingredient remainder can be satisfied before
            // mutating any state.
            foreach (ThingDefCountClass need in fromPlaced)
            {
                if (CountInPlaced(need.thingDef) < need.count)
                    return false;
            }

            // Commit ledger debits and ingredient consumption.
            foreach (KeyValuePair<ThingDef, int> kv in pendingDebit)
                refundLedger[kv.Key] -= kv.Value;
            if (fromPlaced.Count > 0)
                ConsumeFromPlacedIngredients(fromPlaced);
            return true;
        }

        public override string GetReport()
        {
            if (phaseReport != null)
                return phaseReport;

            string weaponLabel = weapon?.LabelShortCap
                ?? job.GetTarget(WeaponIndex).Thing?.LabelShortCap
                ?? "UWU_WeaponFallback".Translate();

            if (spec != null && currentOpIndex >= 0 && currentOpIndex < spec.operations.Count)
            {
                CustomizationOp op = spec.operations[currentOpIndex];
                switch (op.type)
                {
                    case OpType.AddTrait:
                        return "UWU_AddingTrait".Translate(
                            op.trait.LabelCap, weaponLabel);
                    case OpType.RemoveTrait:
                        return "UWU_RemovingTrait".Translate(
                            op.trait.LabelCap, weaponLabel);
                    case OpType.ApplyCosmetics:
                        return "UWU_ApplyingCosmetics".Translate(weaponLabel);
                }
            }

            return "UWU_CustomizingWeapon".Translate(weaponLabel);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Cache workbench comps for per-tick checks and fuel consumption
            CompPowerTrader powerComp = Workbench.TryGetComp<CompPowerTrader>();
            CompRefuelable fuelComp = Workbench.TryGetComp<CompRefuelable>();

            // Weapon destroyed mid-job. weapon is null during the acquire/carry phase;
            // only check once set. Checked before workbench so simultaneous losses
            // surface the weapon — the more player-relevant outcome.
            this.FailOn(() =>
            {
                if (weapon == null || !weapon.Destroyed)
                    return false;
                SetBailMessage("UWU_BailWeaponLost".Translate(WeaponLabel));
                return true;
            });
            // Workbench gone (despawned, destroyed, forbidden, or walled off).
            // Covers both gotoWorkbench calls (carrying weapon, and carrying the
            // ingredient back) plus any phase where the workbench must remain
            // reachable to continue.
            this.FailOn(() =>
            {
                Thing wb = job.GetTarget(WorkbenchIndex).Thing;
                if (wb == null || !wb.Spawned || wb.IsForbidden(pawn))
                {
                    SetBailMessage("UWU_BailWorkbenchUnavailable".Translate(WeaponLabel));
                    return true;
                }
                if (!pawn.CanReach(wb, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    SetBailMessage("UWU_BailWorkbenchUnreachable".Translate(WeaponLabel));
                    return true;
                }
                return false;
            });
            // Workbench loses power or runs out of fuel mid-job.
            this.FailOn(() =>
            {
                bool inactive = (powerComp != null && !powerComp.PowerOn)
                    || (fuelComp != null && !fuelComp.HasFuel);
                if (inactive)
                    SetBailMessage("UWU_BailWorkbenchInactive".Translate(WeaponLabel));
                return inactive;
            });
            AddFinishAction(delegate(JobCondition condition)
            {
                CustomizationSpec.Clear(pawn);

                // Spawn any remaining refund surplus (credits not consumed by additions)
                // Floor fractional amounts here — this is the only point where rounding occurs
                if (refundLedger.Count > 0 && pawn.Map != null)
                {
                    var surplus = new List<ThingDefCountClass>();
                    foreach (KeyValuePair<ThingDef, float> kv in refundLedger)
                    {
                        int whole = Mathf.FloorToInt(kv.Value);
                        if (whole > 0)
                            surplus.Add(new ThingDefCountClass(kv.Key, whole));
                    }
                    if (surplus.Count > 0)
                    {
                        Building bench = Workbench;
                        IntVec3 spawnPos = (bench != null && bench.Spawned)
                            ? bench.Position : pawn.Position;
                        WeaponModificationUtility.SpawnResourcesNear(
                            pawn.Map, spawnPos, surplus);
                    }
                    refundLedger.Clear();
                }

                // Surface the recorded bail reason as a transient top-left message.
                // Restricted to Incompletable so player-driven interrupts (drafting,
                // cancelling) and dev-error paths (Errored) stay silent.
                if (condition == JobCondition.Incompletable
                    && !string.IsNullOrEmpty(bailMessage))
                {
                    Messages.Message(bailMessage, pawn,
                        MessageTypeDefOf.NegativeEvent, historical: false);
                }

                // On interruption, queue a follow-up job so the pawn walks
                // back to retrieve the weapon naturally (no teleporting).
                QueueWeaponRecovery();
            });

            // === ACQUIRE WEAPON ===
            // Derive returnMode from the weapon's current location and move it
            // into the pawn's carryTracker. For equipped/inventory weapons this
            // is a direct transfer (no ground drop). For ground weapons the
            // subsequent toils handle goto + pickup.

            Toil acquireWeapon = ToilMaker.MakeToil("MakeNewToils");
            acquireWeapon.initAction = delegate
            {
                Thing w = job.GetTarget(WeaponIndex).Thing;
                if (w == null || w.Destroyed)
                {
                    SetBailMessage("UWU_BailWeaponLost".Translate(WeaponLabel));
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                weapon = w;

                if (pawn.equipment?.Primary == w)
                {
                    returnMode = WeaponReturnMode.Reequip;
                    pawn.equipment.Remove((ThingWithComps)w);
                    if (!pawn.carryTracker.TryStartCarry(w))
                    {
                        pawn.equipment.AddEquipment((ThingWithComps)w);
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }
                }
                else if (pawn.inventory?.innerContainer?.Contains(w) == true)
                {
                    returnMode = WeaponReturnMode.ReturnToInventory;
                    pawn.inventory.innerContainer.Remove(w);
                    if (!pawn.carryTracker.TryStartCarry(w))
                    {
                        pawn.inventory.innerContainer.TryAdd(w);
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }
                }
                else if (w.Spawned)
                {
                    returnMode = WeaponReturnMode.LeaveOnWorkbench;
                    // Ground weapon — subsequent toils handle goto + pickup
                }
                else
                {
                    Log.Error("[Unique Weapons Unbound] Weapon is in unknown state at job start.");
                    EndJobWith(JobCondition.Errored);
                }
            };
            acquireWeapon.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return acquireWeapon;

            // If pawn is already carrying the weapon (equipped/inventory path),
            // skip the goto + pickup toils and jump straight to the workbench.
            Toil gotoWorkbench = Toils_Goto.GotoThing(WorkbenchIndex, PathEndMode.InteractionCell);

            yield return Toils_Jump.JumpIf(gotoWorkbench,
                () => pawn.carryTracker.CarriedThing != null);

            // Ground weapon path: walk to weapon, pick it up
            yield return Toils_Goto.GotoThing(WeaponIndex, PathEndMode.ClosestTouch)
                .FailOn(() =>
                {
                    Thing w = job.GetTarget(WeaponIndex).Thing;
                    bool gone = w == null || !w.Spawned || w.IsForbidden(pawn)
                        || !pawn.CanReach(w, PathEndMode.ClosestTouch, Danger.Deadly);
                    if (gone)
                        SetBailMessage("UWU_BailWeaponInaccessible".Translate(WeaponLabel));
                    return gone;
                });

            yield return Toils_Haul.StartCarryThing(WeaponIndex);

            // === HAUL TO WORKBENCH & PLACE ===

            yield return gotoWorkbench;

            Toil placeWeapon = ToilMaker.MakeToil("MakeNewToils");
            placeWeapon.initAction = delegate
            {
                string label = weapon?.LabelShortCap ?? "UWU_WeaponFallback".Translate();
                phaseReport = "UWU_PlacingWeapon".Translate(label, Workbench.LabelShortCap);

                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried != null)
                {
                    if (!pawn.carryTracker.TryDropCarriedThing(
                            Workbench.Position, ThingPlaceMode.Direct, out _))
                        pawn.carryTracker.TryDropCarriedThing(
                            Workbench.Position, ThingPlaceMode.Near, out _);

                    pawn.Reserve(weapon, job);
                    pawn.Map.physicalInteractionReservationManager.Reserve(pawn, job, weapon);
                }
            };
            placeWeapon.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return placeWeapon;

            // === DIALOG PHASE ===
            // The weapon is now on the workbench. Open the customization dialog.
            // The dialog's forcePause keeps the game paused while the player
            // makes choices. On the first tick after the dialog closes, we
            // check whether a spec was stored (confirm) or not (cancel).

            Toil waitForDialog = ToilMaker.MakeToil("MakeNewToils");
            waitForDialog.initAction = delegate
            {
                if (weapon == null || weapon.Destroyed)
                {
                    SetBailMessage("UWU_BailWeaponLost".Translate(WeaponLabel));
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                Find.WindowStack.Add(
                    new Dialog_WeaponCustomization(pawn, weapon, Workbench));
            };
            waitForDialog.tickAction = delegate
            {
                // forcePause prevents ticking while the dialog is open.
                // Once it closes and the game unpauses, check the outcome.
                if (CustomizationSpec.Peek(pawn) != null)
                {
                    ReadyForNextToil();
                }
                else if (!Find.WindowStack.IsOpen<Dialog_WeaponCustomization>())
                {
                    // Dialog was cancelled — end job gracefully
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            waitForDialog.defaultCompleteMode = ToilCompleteMode.Never;
            yield return waitForDialog;

            // === CONSUME SPEC + FIND INGREDIENTS ===

            Toil consumeSpec = ToilMaker.MakeToil("MakeNewToils");
            consumeSpec.initAction = delegate
            {
                spec = CustomizationSpec.Retrieve(pawn);
                if (spec == null)
                {
                    Log.Error("[Unique Weapons Unbound] Spec was null at job start.");
                    EndJobWith(JobCondition.Errored);
                    return;
                }
                // Ingredients were reserved synchronously when the player confirmed
                // the dialog (under forcePause). job.targetQueueA / countQueue are
                // already populated; this toil is just the boundary between dialog
                // phase and haul phase.
            };
            consumeSpec.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return consumeSpec;

            // === HAUL PHASE ===
            // targetQueueA is populated at runtime by the consume-spec toil,
            // so we use a jump to skip hauling when there are no ingredients
            // rather than a compile-time conditional.

            // Define startWorkLoop ahead so the jump can reference it
            Toil startWorkLoop = ToilMaker.MakeToil("MakeNewToils");
            startWorkLoop.initAction = delegate
            {
                currentOpIndex = 0;
                phaseReport = null; // Clear so per-op GetReport logic takes over
            };
            startWorkLoop.defaultCompleteMode = ToilCompleteMode.Instant;

            yield return Toils_Jump.JumpIf(startWorkLoop,
                () => job.targetQueueA == null || job.targetQueueA.Count == 0);

            Toil setHaulReport = ToilMaker.MakeToil("MakeNewToils");
            setHaulReport.initAction = delegate
            {
                string label = weapon?.LabelShortCap ?? "UWU_WeaponFallback".Translate();
                phaseReport = "UWU_GatheringMaterials".Translate(label);
            };
            setHaulReport.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return setHaulReport;

            Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(IngredientIndex);
            yield return extract;

            yield return Toils_Goto.GotoThing(IngredientIndex, PathEndMode.ClosestTouch)
                .FailOn(() =>
                {
                    Thing ing = job.GetTarget(IngredientIndex).Thing;
                    if (ing == null || !ing.Spawned || ing.IsForbidden(pawn))
                    {
                        SetBailMessage("UWU_BailIngredientLost".Translate(WeaponLabel));
                        return true;
                    }
                    // Unreachable (e.g. a wall went up between pawn and stockpile).
                    // Caught here so the job ends Incompletable with our message
                    // rather than vanilla's silent Errored path from the pather.
                    if (!pawn.CanReach(ing, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        SetBailMessage("UWU_BailIngredientUnreachable".Translate(WeaponLabel));
                        return true;
                    }
                    return false;
                });

            yield return Toils_Haul.StartCarryThing(IngredientIndex, putRemainderInQueue: true);

            yield return Toils_Goto.GotoThing(WorkbenchIndex, PathEndMode.InteractionCell);

            Toil dropIngredient = ToilMaker.MakeToil("MakeNewToils");
            dropIngredient.initAction = delegate
            {
                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried == null)
                    return;

                // Track placed stacks via the placedAction callback rather than
                // the out resultingThing parameter. This mirrors the vanilla bill
                // pattern (Toils_Haul.PlaceHauledThingInCell) and is robust to
                // mods like Stack Gap that patch GenPlace.TryPlaceDirect and
                // leave resultingThing null on their spawn-new-stack paths while
                // still invoking the callback for every placed/absorbed stack.
                Action<Thing, int> placedAction = delegate(Thing placed, int _)
                {
                    if (placed != null && !placedIngredients.Contains(placed))
                        placedIngredients.Add(placed);
                };

                IntVec3 cell = FindIngredientPlaceCell(carried);
                if (!pawn.carryTracker.TryDropCarriedThing(cell, ThingPlaceMode.Direct, out _, placedAction))
                    pawn.carryTracker.TryDropCarriedThing(Workbench.Position, ThingPlaceMode.Near, out _, placedAction);
            };
            dropIngredient.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return dropIngredient;

            yield return Toils_Jump.JumpIfHaveTargetInQueue(IngredientIndex, extract);

            // === WORK LOOP ===
            // Each operation is applied after its work tick completes, so each
            // completed step leaves the weapon in a consistent state. Def
            // conversions (base↔unique) are bundled into the trait operation
            // that crosses the 0↔1 trait boundary.

            yield return startWorkLoop;

            // Pre-flight: verify the upcoming op can be paid before sinking
            // 1000 ticks of work into it. Catches placed-ingredient losses
            // (fire, deterioration, etc.) at the start of each iteration so the
            // pawn doesn't waste a full work cycle on a cost we already can't pay.
            Toil precheckOp = ToilMaker.MakeToil("MakeNewToils");
            precheckOp.initAction = delegate
            {
                if (spec == null || currentOpIndex >= spec.operations.Count)
                    return;
                CustomizationOp op = spec.operations[currentOpIndex];
                if (!CanAffordOpCost(op.cost))
                {
                    RecordShortfallBail(op.trait);
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            precheckOp.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return precheckOp;

            Toil workToil = Toils_General.Wait(WorkTicksPerOp, WorkbenchIndex);
            workToil.AddPreTickAction(delegate
            {
                if (fuelComp != null)
                    fuelComp.Notify_UsedThisTick();
            });
            workToil.WithProgressBar(WorkbenchIndex,
                () => 1f - (float)ticksLeftThisToil / WorkTicksPerOp);
            yield return workToil;

            Toil applyOp = ToilMaker.MakeToil("MakeNewToils");
            applyOp.initAction = delegate
            {
                if (currentOpIndex < spec.operations.Count)
                {
                    ApplyOperation(spec.operations[currentOpIndex]);
                    currentOpIndex++;
                }
            };
            applyOp.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return applyOp;

            yield return Toils_Jump.JumpIf(precheckOp, () => currentOpIndex < spec.operations.Count);

            // === FINALIZE ===

            Toil finalize = ToilMaker.MakeToil("MakeNewToils");
            finalize.initAction = delegate
            {
                // Final Setup call for ability prop wiring
                CompUniqueWeapon uniqueComp = weapon.TryGetComp<CompUniqueWeapon>();
                uniqueComp?.Setup(false);

                // Apply final color after Setup() to ensure it sticks
                if (spec.finalColor != null && uniqueComp != null)
                    WeaponModificationUtility.SetColor(weapon, spec.finalColor);
            };
            finalize.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finalize;

            // === PAUSE: brief moment before returning the finished weapon ===

            Toil admire = Toils_General.Wait(300, WorkbenchIndex);
            admire.initAction = delegate
            {
                phaseReport = "UWU_AdmiringWork".Translate();
            };
            yield return admire;

            // === RETURN WEAPON ===
            // Post-completion behavior is driven by returnMode, derived during
            // the acquire phase from the weapon's original location.

            Toil returnWeaponToil = ToilMaker.MakeToil("MakeNewToils");
            returnWeaponToil.initAction = delegate
            {
                QueueWeaponRecovery();
                weapon = null; // Prevent finish action from double-recovering
            };
            returnWeaponToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return returnWeaponToil;
        }

        /// <summary>
        /// Queues a follow-up job so the pawn walks to the weapon and picks it
        /// up via the standard equip/take-inventory job drivers. Used for both
        /// normal completion (pawn is at workbench, job completes near-instantly)
        /// and interruption recovery (pawn walks back to retrieve weapon).
        /// </summary>
        private void QueueWeaponRecovery()
        {
            if (weapon == null || weapon.Destroyed)
                return;

            if (pawn.Map == null)
                return;

            // Drop from carry if the pawn is still holding the weapon
            if (pawn.carryTracker?.CarriedThing == weapon)
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);

            if (!weapon.Spawned || weapon.Destroyed)
                return;

            switch (returnMode)
            {
                case WeaponReturnMode.Reequip:
                    pawn.jobs.jobQueue.EnqueueFirst(
                        JobMaker.MakeJob(JobDefOf.Equip, weapon));
                    break;

                case WeaponReturnMode.ReturnToInventory:
                    Job takeJob = JobMaker.MakeJob(JobDefOf.TakeInventory, weapon);
                    takeJob.count = 1;
                    pawn.jobs.jobQueue.EnqueueFirst(takeJob);
                    break;

                case WeaponReturnMode.LeaveOnWorkbench:
                    break;
            }
        }

        private void ApplyOperation(CustomizationOp op)
        {
            switch (op.type)
            {
                case OpType.RemoveTrait:
                    // Negative-trait removals carry a cost (op.cost). Pay it before
                    // removing the trait so a placed-ingredient shortfall can't leave
                    // the trait already gone with no payment recorded.
                    if (!TryConsumeOpCost(op.cost))
                    {
                        RecordShortfallBail(op.trait);
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    WeaponModificationUtility.RemoveTrait(weapon, op.trait);

                    // Credit refund to the virtual ledger atomically with the removal.
                    // Raw costs are stored on the op; apply CostMultiplier and RefundRate
                    // here as float to defer rounding until resources are actually spawned
                    // or consumed.
                    if (op.refund != null)
                    {
                        foreach (ThingDefCountClass refund in op.refund)
                        {
                            float credit = refund.count
                                * TraitCostUtility.CostMultiplier
                                * TraitCostUtility.RefundRate;
                            if (refundLedger.ContainsKey(refund.thingDef))
                                refundLedger[refund.thingDef] += credit;
                            else
                                refundLedger[refund.thingDef] = credit;
                        }
                    }

                    // If removing the last trait, convert unique→base atomically
                    CompUniqueWeapon removeComp = weapon.TryGetComp<CompUniqueWeapon>();
                    if (removeComp != null && removeComp.TraitsListForReading.Count == 0
                        && UWU_Mod.Settings.allowDefConversion)
                    {
                        ThingDef baseDef = WeaponRegistry.GetBaseVariant(weapon.def);
                        if (baseDef != null)
                            ConvertWeaponInPlace(baseDef);
                    }
                    break;

                case OpType.ApplyCosmetics:
                    if (weapon.TryGetComp<CompUniqueWeapon>() != null)
                    {
                        if (op.nameToApply != null)
                            WeaponModificationUtility.SetName(weapon, op.nameToApply);
                        if (op.textureIndexToApply.HasValue)
                            WeaponModificationUtility.SetTextureIndex(weapon, op.textureIndexToApply.Value);
                    }
                    break;

                case OpType.AddTrait:
                    // Pay the cost first — if placed ingredients have been destroyed
                    // (fire, explosion, deterioration), abort cleanly before any
                    // mutation (def conversion, trait add) leaves a partial state.
                    if (!TryConsumeOpCost(op.cost))
                    {
                        RecordShortfallBail(op.trait);
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    // If weapon is currently base, convert base→unique first
                    if (!WeaponRegistry.IsUniqueWeapon(weapon.def) && UWU_Mod.Settings.allowDefConversion)
                    {
                        ThingDef uniqueDef = WeaponRegistry.GetUniqueVariant(weapon.def);
                        if (uniqueDef != null)
                            ConvertWeaponInPlace(uniqueDef);
                    }

                    WeaponModificationUtility.AddTrait(weapon, op.trait);

                    // Apply bundled cosmetics (merged from a cosmetics op that
                    // would have been a no-op when the weapon was in base state)
                    if (weapon.TryGetComp<CompUniqueWeapon>() != null)
                    {
                        if (op.nameToApply != null)
                            WeaponModificationUtility.SetName(weapon, op.nameToApply);
                        if (op.textureIndexToApply.HasValue)
                            WeaponModificationUtility.SetTextureIndex(weapon, op.textureIndexToApply.Value);
                    }
                    break;
            }

            // Apply color change if this op carries one
            if (weapon.TryGetComp<CompUniqueWeapon>() != null)
            {
                if (op.clearColor)
                    WeaponModificationUtility.SetColor(weapon, null);
                else if (op.colorToApply != null)
                    WeaponModificationUtility.SetColor(weapon, op.colorToApply);
            }
        }

        /// <summary>
        /// Converts the weapon to a different ThingDef in-place (base↔unique).
        /// Destroys the current weapon, spawns a new one at the same position,
        /// and updates reservations. Called atomically within an ApplyOperation
        /// step when a trait change crosses the 0↔1 boundary.
        /// </summary>
        private void ConvertWeaponInPlace(ThingDef targetDef)
        {
            Thing newWeapon = WeaponModificationUtility.ConvertWeaponDef(weapon, targetDef);
            IntVec3 pos = weapon.Position;
            Map map = weapon.Map;

            // Transfer relic status BEFORE destroying the old weapon so that
            // Thing.Destroy() does not fire Notify_ThingLost on the precept.
            WeaponModificationUtility.TransferRelicStatus(weapon, newWeapon);

            if (weapon.Spawned)
                weapon.Destroy();
            else if (!weapon.Destroyed)
                weapon.Destroy();

            // Apply desired texture on base→unique so it doesn't flash a random variant
            if (WeaponRegistry.IsUniqueWeapon(targetDef)
                && spec.finalTextureIndex.HasValue)
                WeaponModificationUtility.SetTextureIndex(newWeapon, spec.finalTextureIndex.Value);

            GenSpawn.Spawn(newWeapon, pos, map);
            pawn.Reserve(newWeapon, job);
            pawn.Map.physicalInteractionReservationManager.Reserve(pawn, job, newWeapon);
            weapon = newWeapon;
        }
    }
}
