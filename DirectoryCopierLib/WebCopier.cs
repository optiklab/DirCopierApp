/*
 * COPYRIGHT 2016 Anton Yarkov
 * 
 * Email: anton.yarkov@gmail.com
 * 
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace DirectoryCopierLib
{
    public class WebCopier
    {
        #region Public methods

        public event ActionHappenedEventHandler ActionHappened;

        public bool CopyWebDirectoryToFolder(string sourceUri, string targetDirPath)
        {
            Uri uri = null;
            DirectoryInfo targetDir = null;
            try
            {
                uri = new Uri(sourceUri);
                targetDir = new DirectoryInfo(targetDirPath);
            }
            catch (Exception ex)
            {
                ActionHappened(this, new ActionHappenedEventArgs("Can't get access to directory! Exception details: " + ex.Message + " " + ex.StackTrace));
            }

            if (uri == null || targetDir == null)
            {
                ActionHappened(this, new ActionHappenedEventArgs("One of specified directory is incorrect!"));

                return false;
            }

            if (IsTargetFoundInsideSource(uri, targetDir))
            {
                ActionHappened(this, new ActionHappenedEventArgs("Target folder is already contains Source folder!"));

                return false;
            }

            CopyStructure(uri, targetDirPath);

            ActionHappened(this, new ActionHappenedEventArgs("Copying finished."));

            return true;
        }

        #endregion

        #region Private methods

        private bool IsTargetFoundInsideSource(Uri sourceDir, DirectoryInfo targetDir)
        {
            var dirs = targetDir.GetDirectories();

            string sourceDirName = GetSourceRootFragment(sourceDir.AbsolutePath);

            return dirs.Any(x => x.Name == sourceDirName);
        }

        private string GetSourceRootFragment(string path)
        {
            string root = string.Empty;

            if (!string.IsNullOrWhiteSpace(path))
            {
                int index = path.TrimEnd('/').LastIndexOf(@"/");

                root = path.Substring(index).Trim('/');
            }

            return root;
        }

        private bool IsFile(string fragment)
        {
            Regex regex = new Regex("^.*\\.[^\\\\]+$"); // "\\\"([^\"]*)\\\"");
            MatchCollection matches = regex.Matches(fragment);
            if (matches.Count > 0)
            {
                return true;
            }
            return false;
        }

        private DirectoryInfo CreateFolder(string targetDirPath)
        {
            DirectoryInfo di = null;
            try
            {
                di = Directory.CreateDirectory(targetDirPath);

                ActionHappened(this, new ActionHappenedEventArgs(string.Format("Successfully created folder '{0}'.", targetDirPath)));
            }
            catch (Exception ex)
            {
                ActionHappened(this, new ActionHappenedEventArgs(string.Format("Can't create folder '{0}'! Exception details: {1} {2}", targetDirPath, ex.Message, ex.StackTrace)));
            }

            return di;
        }

        private void CopyStructure(Uri sourceDir, string targetDirPath)
        {
            string root = GetSourceRootFragment(sourceDir.ToString());
            
            if (IsFile(root))
            {
                DirectoryInfo di = CreateFolder(targetDirPath);

                if (di == null)
                {
                    return;
                }

                // File to download
                try
                {
                    string targetPath = string.Format(@"{0}\{1}", di.FullName, root);

                    DownloadFile(sourceDir, targetPath);
                }
                catch (Exception ex)
                {
                    ActionHappened(this, new ActionHappenedEventArgs(string.Format("Can't get access to uri '{0}'! Exception details: {1} {2}", sourceDir.ToString(), ex.Message, ex.StackTrace)));
                }
            }
            else
            {
                DirectoryInfo di = CreateFolder(string.Format(@"{0}\{1}", targetDirPath, root));

                if (di == null)
                {
                    return;
                }

                try
                {
                    var childs = ListChilds(sourceDir);

                    if (childs.Any())
                    {
                        // Folder to load childs.
                        foreach (string child in childs)
                        {
                            CopyStructure(new Uri(string.Format(@"{0}\{1}", sourceDir.ToString(), child)),
                                di.FullName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ActionHappened(this, new ActionHappenedEventArgs(string.Format("Can't get access to uri '{0}'! Exception details: {1} {2}", sourceDir.ToString(), ex.Message, ex.StackTrace)));
                }
            }
        }
        
        private IEnumerable<string> ListChilds(Uri source)
        {
            var hrefs = ListHrefs(source, @"<a\s+(?:[^>]*?\s+)?href=""([^ ""]*)""");

            List<string> result = new List<string>();

            if (hrefs != null)
            {
                foreach (string href in hrefs)
                {
                    int index = href.IndexOf(@"href=""");
                    string hrefSubstr = href.Substring(index + 6);

                    index = hrefSubstr.IndexOf(@"""");
                    result.Add(hrefSubstr.Substring(0, index));
                }
            }

            return result;
        }

        private IEnumerable<string> ListHrefs(Uri source, string mask)
        {
            List<string> result = new List<string>();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(source);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string html = reader.ReadToEnd();

                    Regex regex = new Regex(mask);
                    MatchCollection matches = regex.Matches(html);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            if (match.Success)
                            {
                                result.Add(match.ToString());
                            }
                        }
                    }
                }
            }

            return result;
        }

        private void DownloadFile(Uri source, string pathToSave)
        {
            WebClient request = new WebClient();

            byte[] fileData = null;

            try
            {
                ActionHappened(this, new ActionHappenedEventArgs(string.Format("Downloading '{0}'", source.ToString())));

                fileData = request.DownloadData(source);

                if (fileData != null && fileData.Length > 0)
                {
                    File.WriteAllBytes(pathToSave, fileData);
                }

                ActionHappened(this, new ActionHappenedEventArgs(string.Format("Successfully copied '{0}' to '{1}'", source.ToString(), pathToSave)));
            }
            catch (Exception ex)
            {
                ActionHappened(this, new ActionHappenedEventArgs(string.Format("Can't get access to uri '{0}'! Exception details: {1} {2}", source.ToString(), ex.Message, ex.StackTrace)));
            }

        }

        #endregion
    }
}
