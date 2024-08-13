using WatchedFilmsTracker.Source.DataGridHelpers;

namespace WatchedFilmsTracker.Source.ManagingFilmsFile
{
    public class RecordModel
    {
        public List<Cell> Cells { get; set; } = new List<Cell>();

        public RecordModel(List<Cell> cells)
        {
            Cells = cells;
        }

        public string StringToBeSavedInFile()
        {
            return "wip";
            //todo check which columns exist and what order
        }

        public string ToNewString()
        {
            return "wip";
            //todo all
        }
    }
}