using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AppComponents.ControlFlow;
using License = AppComponents.ControlFlow.License;

namespace GenerateLicense
{
    public partial class Form1 : Form
    {
         
        private LicenseGenerator _gen = new LicenseGenerator();
        private bool _keysLoaded = false;
        private bool _templateLoaded = false;
        private string _templateJson;

        public Form1()
        {
            InitializeComponent();
        }

        

        private void buttonLoadKeys_Click(object sender, EventArgs e)
        {
            var fd = new OpenFileDialog();
            PrepMasterKeyFileDialog(fd);

            if (fd.ShowDialog() == DialogResult.OK)
            {
                _gen.LoadKeyFile(fd.FileName);
                KeysAreLoaded();
            }
        }

        private void KeysAreLoaded()
        {
            _keysLoaded = true;
            buttonLoadTemplate.Enabled = true;
            btnSavePublicKey.Enabled = true;
            btnValidateLicense.Enabled = true;
        }

        private void buttonGenerateMasterKeys_Click(object sender, EventArgs e)
        {
            _gen.GenerateLicenseMasterKeys();

            var fd = new SaveFileDialog();
            PrepMasterKeyFileDialog(fd);

            if (fd.ShowDialog() == DialogResult.OK)
            {
                _gen.SaveKeyFile(fd.FileName);
                KeysAreLoaded();
            }
        }

        private static void PrepMasterKeyFileDialog(FileDialog fd)
        {
            fd.AddExtension = true;
            fd.CheckPathExists = true;
            fd.DefaultExt = "key";
            fd.Filter = "Key Files (*.key)|*.key";
            fd.FilterIndex = 1;
            fd.FileName = "MasterLicense.key";
        }

        private void buttonLoadTemplate_Click(object sender, EventArgs e)
        {
            var fd = new OpenFileDialog
                {
                    AddExtension = true,
                    DefaultExt = "json",
                    Filter = "Json Files (*.json)|*.json",
                    FilterIndex = 1
                };
            
                
            if (fd.ShowDialog() == DialogResult.OK)
            {
                _templateJson = File.ReadAllText(fd.FileName);
                _templateLoaded = true;
                btnGenerateLicense.Enabled = true;
                this.tbTemplateText.Text = _templateJson;
            }
        }

        private void btnGenerateLicense_Click(object sender, EventArgs e)
        {
            var newLicense = License.FromTemplate(_templateJson);
            newLicense.Specification.IssueDate = DateTimeOffset.Now;
            newLicense.Specification.Customer = tbCustomer.Text;
            newLicense.Specification.SKUCode = tbSKU.Text;

            _gen.Authorize(newLicense);

            var fd = new SaveFileDialog
                {
                    AddExtension = true,
                    DefaultExt = "license",
                    Filter = "License Files (*.license)|*.license",
                    FilterIndex = 1
                };

            if (fd.ShowDialog() == DialogResult.OK)
            {
                newLicense.Save(fd.FileName);
            }

        }

        private void btnSavePublicKey_Click(object sender, EventArgs e)
        {
             var fd = new SaveFileDialog
                {
                    AddExtension = true,
                    CheckPathExists = true,
                    DefaultExt = "key",
                    Filter = "Key Files (*.key)|*.key",
                    FilterIndex = 1,
                    FileName = "LicensePublic.key"
                };

             if (fd.ShowDialog() == DialogResult.OK)
             {
                 File.WriteAllText(fd.FileName, _gen.PublicKeyInfoXML);
             }
        }

        private void btnValidateLicense_Click(object sender, EventArgs e)
        {
            var fd = new OpenFileDialog
            {
                AddExtension = true,
                DefaultExt = "license",
                Filter = "License Files (*.license)|*.license",
                FilterIndex = 1
            };

            if (fd.ShowDialog() == DialogResult.OK)
            {
                var valLicense = License.Load(fd.FileName);

                

                using (var validator = new LicenseAcceptor(_gen.PublicKeyInfoXML))
                {
                    var validated = validator.Validate(valLicense);
                    MessageBox.Show(string.Format("License Valid: {0}", validated), "License Validation Result",
                                    MessageBoxButtons.OK);
                }
            }

        }
    }
}
