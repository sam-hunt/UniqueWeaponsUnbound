namespace UniqueWeaponsUnbound
{
    // Doubles all costs. Used for traits like akimbo that add a second rendered
    // weapon and double the fire rate. A named alias for CostFactorWorker, whose
    // costFactor defaults to 2, so rules can keep stating the intent rather than
    // the number.
    public class DoubleCostWorker : CostFactorWorker
    {
    }
}
