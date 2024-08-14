using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.ManagingFilmsFile;

namespace WatchedFilmsTracker.Source.Services.Csv
{
    public class CSVwriter
    {
        private string _fileColumnHeaders;
        private string filePath;
        private StreamWriter filmsWriter;

        public CSVwriter(string newFilePath)
        {
            try
            {
                filmsWriter = new StreamWriter(newFilePath, false, System.Text.Encoding.UTF8);
                filmsWriter.NewLine = "\r\n"; // Set custom line ending
            }
            catch (IOException e)
            {
                Console.Error.WriteLine("File not found.");
            }
        }

        public void Close()
        {
            filmsWriter.Close();
        }

        public void SaveListIntoCSV(List<RecordModel> list, DataGrid dataGrid, List<int> visibleColumns)
        {
            var stringBuilder = new StringBuilder();

            // Extract column headers
            var headers = new string[dataGrid.Columns.Count];
            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                headers[i] = dataGrid.Columns[i].Header.ToString();
            }
            stringBuilder.AppendLine(string.Join("\t", headers));

            foreach (RecordModel filmRecord in list)
            {
                var rowValues = new List<string>();

                for (int i = 0; i < visibleColumns.Count; i++)
                {
                    rowValues.Add(filmRecord.Cells[visibleColumns[i]].Value.ToString());
                }
                stringBuilder.AppendLine(string.Join("\t", rowValues));
            }
            filmsWriter.Write(stringBuilder.ToString());
            filmsWriter.Close();
            Debug.WriteLine("Saved and closed");
        }

        public void SetColumnHeaders(string fileColumnHeaders)
        {
            _fileColumnHeaders = fileColumnHeaders;
        }

        private string ToString(string value)
        {
            return value ?? "";
        }
    }
}