/*
 * COPYRIGHT 2016 Anton Yarkov
 * 
 * Email: anton.yarkov@gmail.com
 * 
 */
using System;
using System.Threading;
using System.Windows.Forms;
using DirectoryCopierLib;

namespace DirCopierApp
{
    public partial class frmMain : Form
    {
        #region Private fields

        private string _sourceDir;
        private string _targetDir;

        #endregion

        #region Private constructor

        public frmMain()
        {
            InitializeComponent();
        }

        #endregion

        #region Private handlers
        private void btnSource_Click(object sender, EventArgs e)
        {
            txbSource.Text = OnOpenFolder();
        }

        private void btnTarget_Click(object sender, EventArgs e)
        {
            txbTarget.Text = OnOpenFolder();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            _sourceDir = txbSource.Text;
            _targetDir = txbTarget.Text;

            toolStripStatusLabel.Text = "Processing...";

            var thr = new Thread(StartProcess);
            thr.IsBackground = true;
            thr.Start();
        }

        private void WebCopier_ActionHappened(object sender, ActionHappenedEventArgs e)
        {
            LogToConsole(e.Log);
        }

        private void Copier_ActionHappened(object sender, ActionHappenedEventArgs e)
        {
            LogToConsole(e.Log);
        }

        #endregion

        #region Private helpers

        /// <summary>
        /// Method shows to user Open Folder dialog and returns path to the folder.
        /// </summary>
        /// <returns>Selected path in the Open Folder dialog</returns>
        private string OnOpenFolder()
        {
            string textboxString = String.Empty;
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    textboxString = fbd.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Can't open folder! See Console for more information.");
            }

            return textboxString;
        }

        private void StartProcess()
        {
            try
            {
                if (_sourceDir.StartsWith("http") || _sourceDir.StartsWith("ftp"))
                {
                    CopyingFromUri();
                }
                else
                {
                    CopyingInFileSystem();
                }
            }
            catch (Exception ex)
            {
                LogToConsole(string.Format("Unexpected error occured! Exception details: {0} {1}", ex.Message, ex.StackTrace));
            }
        }

        private void CopyingFromUri()
        {
            WebCopier webCopier = null;
            try
            {
                webCopier = new WebCopier();

                Application.DoEvents();

                webCopier.ActionHappened += WebCopier_ActionHappened;

                if (!webCopier.CopyWebDirectoryToFolder(_sourceDir, _targetDir))
                {
                    MessageBox.Show("Cannot copy from url! Read details in console!");
                }
            }
            catch (Exception ex)
            {
                LogToConsole(string.Format("Unexpected error occured! Exception details: {0} {1}", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (webCopier != null)
                {
                    webCopier.ActionHappened -= WebCopier_ActionHappened;
                }
            }
        }

        private void CopyingInFileSystem()
        {
            Copier copier = null;
            try
            {
                copier = new Copier();

                Application.DoEvents();

                copier.ActionHappened += Copier_ActionHappened;

                if (!copier.CopyFilesystem(_sourceDir, _targetDir))
                {
                    MessageBox.Show("Cannot copy directory! Read details in console!");
                }
            }
            catch (Exception ex)
            {
                LogToConsole(string.Format("Unexpected error occured! Exception details: {0} {1}", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (copier != null)
                {
                    copier.ActionHappened -= Copier_ActionHappened;
                }
            }
        }

        /// <summary>
        /// Puts additional string into ListBox.
        /// </summary>
        private void LogToConsole(string line)
        {
            lbxConsole.Invoke((MethodInvoker)delegate {
                lbxConsole.Items.Add(line);

                if (line == "Copying finished.")
                {
                    toolStripStatusLabel.Text = "";
                }
            });
        }

        #endregion
    }
}
