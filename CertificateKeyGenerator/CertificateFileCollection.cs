﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using System.Xml.Linq;
using System.Security.Cryptography.X509Certificates;

namespace CertificateKeyGenerator
{
    public class CertificateFileCollection
    {
        public int DuplicateFilesFound { get; private set; }
        public string SourceDirectory { get; private set; }
        public IEnumerable<CertificateFile> CertificateFiles { get; private set; }

        private bool privateKey;
        private string searchExtension;

        public CertificateFileCollection(string searchDirectory, bool searchPrivateKeys)
        {
            if (string.IsNullOrWhiteSpace(searchDirectory)) { throw new ArgumentException("Argument path must not be null, empty or whitespace", "path"); }
            if (!Directory.Exists(searchDirectory)) { throw new DirectoryNotFoundException("Path must exist: " + searchDirectory); }

            this.privateKey = searchPrivateKeys;
            this.SourceDirectory = searchDirectory;
            this.searchExtension = searchExtension = privateKey ? CertificateFile.PrivateKeyFileExtension : CertificateFile.PublicKeyFileExtension;

            IEnumerable<string> filePaths = Directory.EnumerateFiles(SourceDirectory, searchExtension, SearchOption.TopDirectoryOnly);
            this.CertificateFiles = filePaths.Select(file => new CertificateFile(file));
        }

        public CertificateFileCollection(IEnumerable<CertificateFile> certificates)
        {
            CertificateFiles = certificates;
        }

        public List<string> GetPublicKeys()
        {
            int dupes = 0;
            Dictionary<string, string> certificateDictionary = new Dictionary<string, string>();
            foreach (CertificateFile cert in CertificateFiles)
            {
                string thumbprint = cert.GetThumbprint();
                if (!certificateDictionary.ContainsKey(thumbprint))
                {
                    certificateDictionary.Add(thumbprint, cert.GetPublicKey());
                }
                else
                {
                    dupes++;
                }
            }
            DuplicateFilesFound = dupes;
            return certificateDictionary.Values.Select(val => val).ToList();
        }

        public List<string> GetPrivateKeys()
        {
            List<string> results = new List<string>();
            foreach (CertificateFile cert in CertificateFiles)
            {
                results.Add(cert.GetPrivateKey());
            }
            return results;
        }

        public List<string> GetPsAndQs()
        {
            List<string> results = new List<string>();
            foreach (CertificateFile cert in CertificateFiles)
            {
                results.AddRange(EncodingUtility.ExtractPandQ(cert.GetPrivateKey()));
            }
            return results;
        }

        public List<string> GetKeys()
        {
            if (privateKey)
            {
                return GetPrivateKeys();
            }
            else
            {
                return GetPublicKeys();
            }
        }

        public void RemoveAllFiles()
        {
            using (Process proc = new Process())
            {
                ProcessStartInfo procStartInfo = new ProcessStartInfo();
                procStartInfo.FileName = "cmd.exe";
                procStartInfo.Arguments = string.Format(@"/c del /q ""{0}""", SourceDirectory);
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                procStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                proc.StartInfo = procStartInfo;
                proc.Start();
                proc.WaitForExit();
            }
            CertificateFiles = new CertificateFile[] { };
        }
    }
}
