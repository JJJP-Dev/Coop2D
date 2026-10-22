// What a stream of the run's seed is for (#75). Each one has its own numbers, so drawing more from one
// never changes another: a new roll in the waves leaves the upgrade offers as they were.
// The numbers are written out because they pick the stream: changing one changes what every seed gives.
// Add new streams with new numbers; never reuse or renumber one.
public enum RandomStream
{
    Waves = 1,
    Rooms = 2,

    // One per player: the offers of slot n come from stream Offers + n (see RunRandomService.GetOfferStream).
    // That takes 100 to 103, so a new stream goes below 100 or above 103.
    Offers = 100,
}
