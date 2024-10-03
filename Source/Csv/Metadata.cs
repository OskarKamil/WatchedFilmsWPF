using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchedFilmsTracker.Source.Csv
{
    public class Metadata
    {
        public string AllComment { get; set; }
        public string Comment { get; set; }
        public int CommentLines { get; set; }
        public string Filepath { get; set; }
        public string ProgramVersion { get; set; }

        public Dictionary<string, string> Values { get; set; }

        public Metadata(string filepath)
        {
            Filepath = filepath;
            CommentLines = ReadAllCommentLines().Count();
        }

        public List<string> ReadAllCommentLines()
        {
            // read all lines starting with #
            // save all of them to allcomment,
            //save compatible to comment
            
             AllComment = null;

            return null;
        }
    }
}