namespace CorpFleet.Interfaces
{
  
    public interface IStorable
    {
        string FilePath { get; }
        void Save();
    }
}
