using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CadFoundation
{

    public class DirectoryManager
    {  // From GitHubGist: https://gist.github.com/PaulSchrum/4fb6015d46d79c06b08acb7f1bb00c53
        // If I add other things (like createDir or move, etc. The version here should be updated.
        public string GetPathAndAppendFilename(string filename = null)
        {
            if (filename == null || filename.Length == 0)
                return this.path;
            return this.path + "\\" + filename;
        }

        public string path { get; protected set; }
        public List<string> pathAsList
        {
            get
            {
                return this.path.Split('\\').ToList();
            }
        }

        protected void setPathFromList(List<string> aList)
        {
            this.path = string.Join("\\", aList);
        }

        public DirectoryManager()
        {
            this.path = System.IO.Directory.GetCurrentDirectory();
        }

        public int depth
        {
            get
            {
                return pathAsList.Count - 1;
            }
        }

        public DirectoryManager Clone()
        {
            var returnValue = new DirectoryManager();
            returnValue.path = this.path;
            return returnValue;
        }

        public DirectoryManager CdUp(int upSteps)
        {
            if (upSteps > this.depth) throw new IOException("Can't cd up that high.");
            var wd = this.pathAsList.Take(depth - upSteps).ToList();
            this.setPathFromList(wd);
            return this;
        }

        public DirectoryManager CdDown(string directoryName, bool createIfNeeded = false)
        {
            bool needToCreate = false;
            if (!this.ListSubDirectories.Contains(directoryName))
            {
                if (!createIfNeeded)
                    throw new DirectoryNotFoundException();
                else
                    needToCreate = true;
            }
            if (needToCreate)
                System.IO.Directory.CreateDirectory(this.path + "\\" + directoryName);
            var tempList = this.pathAsList;
            tempList.Add(directoryName);
            this.setPathFromList(tempList);
            return this;
        }

        /// <summary>
        /// Looks inside current directory and returns true if the item exists.
        /// False otherwise. It does not alter the directory. It just looks.
        /// </summary>
        /// <param name="subElement">Name of subdirectory or file to inquire about</param>
        /// <returns>True if the subElement exists. False otherwise.</returns>
        public bool ConfirmExists(string subElement)
        {
            var itemName = subElement.Split("\\").LastOrDefault();
            if (ListFiles().Contains(itemName))
                return true;
            return this.ListSubDirectories.Contains(itemName);
        }

        public IReadOnlyList<string> ListSubDirectories
        {
            get
            {
                var v = Directory.GetDirectories(this.path)
                    .Select(s => s.Split('\\').Last())
                    .ToList();
                return v;
            }
        }

        public override string ToString()
        {
            return this.path;
        }

        public void EnsureExists()
        {
            if (!Directory.Exists(this.path))
                Directory.CreateDirectory(this.path);
        }

        public IReadOnlyList<string> ListFiles()
        {
            return (IReadOnlyList<string>)Directory.GetFiles(this.path)
                .Select(fn => Path.GetFileName(fn))
                .ToList();
        }

        /// <summary>
        /// Warning: This method removes the current directory and everything under it
        /// without asking. Use with caution.
        /// </summary>
        /// <param name="subDir"></param>
        internal void ForceRemove(string subDir)
        {
            //throw new NotImplementedException("Okay, really just not tested. So test it.");
            var target = this.Clone();

            foreach (var fileName in target.ListFiles())
            {
                var fullName = target.GetPathAndAppendFilename(fileName);
                System.IO.File.Delete(fullName);
            }

            foreach (var dirName in target.ListSubDirectories)
                target.ForceRemove(dirName);

            System.IO.Directory.Delete(target.ToString());
        }

        public static DirectoryManager FromPwd()
        {
            return new DirectoryManager();
        }

        public static DirectoryManager FromPathString(string str)
        {
            DirectoryManager retVal = new DirectoryManager();
            retVal.path = str;
            return retVal;
        }
    }
}
