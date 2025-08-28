using System.Text;

namespace WatchedFilmsTracker.Source.Csv
{
    public class Metadata
    {
        public string AllComment { get; set; }
        public string Comment { get; set; }
        public string CommentAfter { get; set; }
        public string CommentBefore { get; set; }
        public int CommentLines { get; set; }
        public string ProgramVersion { get; set; }

        public Dictionary<string, string> Values { get; set; }

        private StringBuilder _comment;
        private StringBuilder _commentAfter;
        private StringBuilder _commentBefore;

        public Metadata(List<string> commentLines)
        {
            _commentBefore = new StringBuilder();
            _commentAfter = new StringBuilder();
            _comment = new StringBuilder();

            CommentBefore = "";
            Comment = "";
            CommentAfter = "";
            ProcessLines(commentLines);
        }

        /// <summary>
        /// Processes the comment lines in CSV file. It also trims the comment lines of tabs because some other programs may rectangularize the file by adding more tabs or separators for the comment.
        /// </summary>
        /// <param name="commentLines"></param>
        private void ProcessLines(List<string> commentLines)
        {
            if (commentLines == null)
                return;

            bool programCommentRead = false;
            _comment.Clear();
            _commentBefore.Clear();
            _commentAfter.Clear();

            foreach (string commentLine in commentLines)
            {
                string trimmedLine = commentLine.TrimEnd(); // only remove trailing tabs/spaces

                if (commentLine.StartsWith("# {\"WatchedFilmsTracker\":") && !programCommentRead)
                {
                    _comment.AppendLine(trimmedLine);
                    programCommentRead = true;
                }
                else if (!programCommentRead)
                {
                    _commentBefore.AppendLine(trimmedLine);
                }
                else
                {
                    _commentAfter.AppendLine(trimmedLine);
                }
            }

            Comment = _comment.ToString().Trim();
            CommentBefore = _commentBefore.ToString().Trim();
            CommentAfter = _commentAfter.ToString().Trim();
        }
    }
}