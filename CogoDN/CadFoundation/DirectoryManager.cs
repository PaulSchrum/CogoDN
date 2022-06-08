using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;

namespace CadFoundation
{

    public class DirectoryManager
    {  // From GitHubGist: https://gist.github.com/PaulSchrum/4fb6015d46d79c06b08acb7f1bb00c53
       // If I add other things (like createDir or move, etc. The version here should be updated.

        protected static string delim = Path.DirectorySeparatorChar.ToString();
        private DriveInfo d;

        public string GetPathAndAppendFilename(string filename = null)
        {
            if (filename == null || filename.Length == 0)
                return this.path;
            return this.path + delim + filename;
        }

        public string PrependPWDifNotAlreadyFullPath(string filename)
        {
            if (null == filename) return null;
            if (filename.Contains(delim, System.StringComparison.InvariantCulture))
                return filename;
            else
                return GetPathAndAppendFilename(filename);
        }

        public string path { get; protected set; }
        public List<string> pathAsList
        {
            get
            {
                return this.path.Split(delim).ToList();
            }
        }

        public DirectoryManager DirectoryPart
        {
            get
            {
                FileAttributes attr = File.GetAttributes(this.path);
                if ((attr & FileAttributes.Directory) != FileAttributes.Directory) // it's a file
                {
                    var lclPath = this.pathAsList.Take(depth);
                    var newDM = DirectoryManager.
                        FromPathString(string.Join(delim, lclPath) + delim);
                    return newDM;
                }
                else
                    return DirectoryManager.FromPathString(this.path);
            }
        }

        protected void setPathFromList(List<string> aList)
        {
            this.path = string.Join(delim, aList) + delim;
        }

        public DirectoryManager()
        {
            this.path = System.IO.Directory.GetCurrentDirectory();
        }

        public DirectoryManager(DriveInfo d)
        {
            this.path = d.RootDirectory.FullName;
        }

        protected DirectoryManager(DirectoryManager other)
        {
            this.path = other.path;
            this.d = other.d;
            this.AccessDenied = other.AccessDenied;
        }

        public int depth
        {
            get
            {
                return pathAsList.Count - 1;
            }
        }

        public bool AccessDenied { get; protected set; } = false;

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

        public DirectoryManager CdUpUntil(Func<string, bool> condition)
        {
            DirectoryManager newOne = new DirectoryManager(this);

            for(int i = this.pathAsList.Count-1; i >= 0; i--)
            {
                string dirName = this.pathAsList[i];
                if(condition(dirName))
                {
                    newOne.setPathFromList(this.pathAsList.Take(i+1).ToList());
                    return newOne;
                }
            }

            return null;
        }

        public int Depth
        {
            get { return this.pathAsList.Count - 1; }
        }

        public DirectoryManager CdRoot()
        {
            return CdUp(Depth - 1);
        }

        public DirectoryManager CdDown(string directoryName, bool createIfNeeded = false)
        {
            bool needToCreate = false;
            var subdirs = this.ListSubDirectories;
            if (!this.ListSubDirectories.Contains(directoryName))
            {
                if (!createIfNeeded)
                    throw new DirectoryNotFoundException();
                else
                    needToCreate = true;
            }
            if (needToCreate)
                System.IO.Directory.CreateDirectory(this.path + delim + directoryName);
            var tempList = this.pathAsList;
            tempList.Add(directoryName);
            this.setPathFromList(tempList);
            return this;
        }

        public List<DirectoryManager> CdDownToFileName(string fileName)
        {
            var returnList = new List<DirectoryManager>();
            string[] files = Directory.GetFiles(this.path, fileName, SearchOption.AllDirectories);
            if (files.Length == 0)
                return returnList;

            foreach(var path in files)
            {
                returnList.Add(DirectoryManager.FromPathString(path));
            }
            return returnList;
        }

        /// <summary>
        /// Looks inside current directory and returns true if the item exists.
        /// False otherwise. It does not alter the directory. It just looks.
        /// </summary>
        /// <param name="subElement">Name of subdirectory or file to inquire about</param>
        /// <returns>True if the subElement exists. False otherwise.</returns>
        public bool ConfirmExists(string subElement)
        {
            var itemName = subElement.Split(delim).LastOrDefault();
            if (ListFiles().Contains(itemName))
                return true;
            return this.ListSubDirectories.Contains(itemName);
        }

        public bool DeleteFile(string filename)
        {
            if (!ConfirmExists(filename))
                return true;

            var fullName = GetPathAndAppendFilename(filename);
            File.Delete(fullName);
            if (ConfirmExists(filename))
                return false;
            return true;
        }

        public IReadOnlyList<string> ListSubDirectories
        {
            get
            {
                var v = Directory.GetDirectories(this.path)
                    .Select(s => s.Split(delim).Last())
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

        public static List<DirectoryManager> ListDrives()
        {
            return DriveInfo.GetDrives().Select(d => new DirectoryManager(d))
                .ToList();
        }

        public void CreateTextFile(string localLogFName)
        {
            var fileToCreate = GetPathAndAppendFilename(localLogFName);
            using (File.Create(fileToCreate)) ;

        }
    }

    internal class DirectoryNode : DirectoryManager
    {
        public List<DirectoryNode> Subdirectories { get; private set; }
        public List<FileNode> Files { get; private set; }
        public long Size { get; private set; } = 0;
        public double SizeMB
        {
            get { return (double)Size / 1048576; }
        }
        public double SizeGB
        {
            get { return (double)Size / 1073741824; }
        }

        public long DirectoryCount { get; private set; } = 0;
        public long FileCount { get; private set; } = 0;

        public DirectoryNode() : base()
        {

        }

        public DirectoryNode(DirectoryManager dm)
        {
            this.path = dm.path;
        }

        public DirectoryNode(string path)
        {
            this.path = path;
        }

        public static DirectoryNode SetAtDriveLetterRoot(string driveLetter)
        {
            if (driveLetter.Length == 1)
                driveLetter += ":";
            var drives = DirectoryManager.ListDrives();
            foreach(var drive in drives)
            {
                if (drive.pathAsList.First().ToUpper() == driveLetter.ToUpper())
                    return new DirectoryNode(driveLetter + delim);
            }

            return null;
        }

        public void PopulateAll()
        {
            Subdirectories = new List<DirectoryNode>();
            if (path.Contains("$RECYCLE.BIN")
                || path.Contains("System Volume Information")
                )
            {
                AccessDenied = true;
                Files = new List<FileNode>();
                Size = 1028;
                DirectoryCount++;
                return;
            }


            foreach(var aDir in Directory.EnumerateDirectories(path))
            {
                Subdirectories.Add(new DirectoryNode(aDir));
            }

            Files = Directory.EnumerateFiles(path)
                .Select(str => FileNode.Create(this, str))
                .ToList();

            foreach(var aFile in Files)
            {
                aFile.Size = new FileInfo(GetPathAndAppendFilename(aFile.Name))
                    .Length;
                this.Size += aFile.Size;
                this.FileCount++;
            }

            foreach(var aDirectory in Subdirectories)
            {
                try
                {
                    aDirectory.PopulateAll();
                }
                catch(UnauthorizedAccessException uae)
                {
                    this.AccessDenied = true;
                }
                Size += aDirectory.Size;
                DirectoryCount += aDirectory.DirectoryCount;
                FileCount += aDirectory.FileCount;
            }
            DirectoryCount += Subdirectories.Count;

            Size += 1028; // Estimated min dir size for NTFS.
        }

        public List<FileNode> allFilesFlat
        {
            get
            {
                var returnList = new List<FileNode>();
                if (this.AccessDenied)
                    return returnList;

                if (null == Files)
                {
                    try
                    {
                        PopulateAll();
                    }
                    catch (UnauthorizedAccessException uae)
                    {
                        this.AccessDenied = true;
                        return returnList;
                    }
                }

                returnList = Files;
                foreach(var dirNode in Subdirectories)
                {
                    returnList.AddRange(dirNode.allFilesFlat);
                }
                return returnList;
            }
        }

        internal IReadOnlyList<string> FindAll(string searchString)
        {
            var returnList = new List<string>();
            
            returnList = this.allFilesFlat
                .Where(f => f.Name.Contains(searchString))
                .Select(f => f.PathAndName)
                .ToList();

            return returnList;
        }

    }

    internal class FileNode : DirectoryManager
    {
        public static FileNode Create(DirectoryNode dir, string fullpathname)
        {
            string filename = fullpathname.Split(delim).Last();
            FileNode returnInstance = new FileNode(dir, filename);
            return returnInstance;
        }

        private FileNode(DirectoryNode parent, string name)
        {
            Name = name;
            this.path = parent.path;
        }

        public string Name { get; set; }
        public long Size { get; set; }
        public string PathAndName
        {
            get { return path + delim + Name; }
        }

        public override string ToString()
        {
            return path + delim + Name;
        }
    }
}
