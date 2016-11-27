/*
 * COPYRIGHT 2016 Anton Yarkov
 * 
 * Email: anton.yarkov@gmail.com
 * 
 */
using System;
using System.Linq;
using System.IO;

namespace DirectoryCopierLib
{
    public class Copier
    {
        #region Public methods

        public event ActionHappenedEventHandler ActionHappened;

        public bool CopyFilesystem(string sourceDirPath, string targetDirPath)
        {
            DirectoryInfo sourceDir = null;
            DirectoryInfo targetDir = null;
            try
            {
                sourceDir = new DirectoryInfo(sourceDirPath);
                targetDir = new DirectoryInfo(targetDirPath);
            }
            catch (Exception ex)
            {
                ActionHappened(this, new ActionHappenedEventArgs("Can't get access to directory! Exception details: " + ex.Message + " " + ex.StackTrace));
            }

            if (sourceDir == null || targetDir == null)
            {
                ActionHappened(this, new ActionHappenedEventArgs("One of specified directory is incorrect!"));

                return false;
            }

            if (IsTargetFoundInsideSource(sourceDir, targetDir))
            {
                ActionHappened(this, new ActionHappenedEventArgs("Target folder is already contains Source folder!"));

                return false;
            }

            CopyStructure(sourceDir, targetDirPath);

            ActionHappened(this, new ActionHappenedEventArgs("Copying finished."));

            return true;
        }

        #endregion

        #region Private methods

        private bool IsTargetFoundInsideSource(DirectoryInfo sourceDir, DirectoryInfo targetDir)
        {
            var dirs = targetDir.GetDirectories();

            return dirs.Any(x => x.Name == sourceDir.Name);
        }

        private void CopyStructure(DirectoryInfo sourceDir, string targetDirPath)
        {
            DirectoryInfo di = Directory.CreateDirectory(string.Format(@"{0}\{1}", targetDirPath, sourceDir.Name));

            ActionHappened(this, new ActionHappenedEventArgs(string.Format("Successfully copied '{0}' to '{1}'", sourceDir.FullName, di.FullName)));

            string fileName = string.Empty;
            try
            {
                var files = sourceDir.GetFiles();

                foreach (FileInfo file in files)
                {
                    fileName = file.FullName;
                    string targetPath = string.Format(@"{0}\{1}", di.FullName, file.Name);

                    file.CopyTo(targetPath);

                    ActionHappened(this, new ActionHappenedEventArgs(string.Format("Successfully copied '{0}' to '{1}'", fileName, targetPath)));
                }
            }
            catch (Exception ex)
            {
                ActionHappened(this, new ActionHappenedEventArgs(string.Format("Can't get access to file '{0}'! Exception details: {1} {2}", fileName, ex.Message, ex.StackTrace)));
            }
            
            string dirName = string.Empty;
            try
            {
                var subDirs = sourceDir.GetDirectories();
                foreach (DirectoryInfo subDir in subDirs)
                {
                    dirName = subDir.FullName;
                    CopyStructure(subDir,
                        string.Format(@"{0}\{1}", di.FullName, subDir.Name));
                }
            }
            catch (Exception ex)
            {
                ActionHappened(this, new ActionHappenedEventArgs(string.Format("Can't get access to sub directory '{0}'! Exception details: {1} {2}", dirName, ex.Message, ex.StackTrace)));
            }
        }

        #endregion
    }
}
