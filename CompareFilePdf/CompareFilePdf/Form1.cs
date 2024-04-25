using CompareFilePDF.Class;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CompareFilePDF
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnOpenFile1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Open File";
                openFileDialog.Filter = "All Files (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFileName = openFileDialog.FileName;

                    txtFile1.Text = selectedFileName;
                }
            }
        }

        private void txtFile1_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnOpenFile2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Open File";
                openFileDialog.Filter = "All Files (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFileName = openFileDialog.FileName;

                    txtFile2.Text = selectedFileName;
                }
            }
        }

        private void btnCompare_Click(object sender, EventArgs e)
        {   
            string filePath1 = txtFile1.Text;
            string filePath2 = txtFile2.Text;
            string outFilePath2 = string.Empty;
            
            Repeat:
            if (string.IsNullOrEmpty(outFilePath2)) 
            {
                MessageBox.Show("Please select the report export file!");
                using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
                {
                    folderBrowserDialog.Description = "Select a folder";

                    // Show the dialog and check if the user clicked OK
                    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Get the selected folder path and display it
                        string selectedFolderPath = folderBrowserDialog.SelectedPath;

                        // Set the TextBox text to the selected folder path
                        outFilePath2 = selectedFolderPath+ "report.pdf";
                        goto Repeat;
                    }
                }
            }
            label1.Text = ComparePDF.ComparePDFFiles(filePath1, filePath2, outFilePath2, 10);
        }
    }
}
