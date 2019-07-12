//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
#region Using directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

#endregion

using Microsoft.Samples.Debugging.MdbgEngine;
using Microsoft.Samples.Debugging.CorDebug;
using Microsoft.Samples.Debugging.CorMetadata;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using Microsoft.Samples.Debugging.CorPublish;
using System.Diagnostics;

namespace gui
{
    partial class AttachProcess : Form
    {
        public AttachProcess()
        {
            InitializeComponent();

            RefreshProcesses();
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            RefreshProcesses();
        }

        class Item
        {
            public Item(int pid, string stName)
            {
                m_stName = stName;
                m_pid = pid;
            }

            public override string ToString()
            {
                return m_stName;
            }

            string m_stName;
            int m_pid;
            public int Pid
            {
                get { return m_pid; }
            }
        }

        void RefreshProcesses()
        {
            this.listBoxProcesses.Items.Clear();
            int count = 0;

            int curPid = System.Diagnostics.Process.GetCurrentProcess().Id;

            foreach (Process p in Process.GetProcesses())
            {

                if (Process.GetCurrentProcess().Id == p.Id)  // let's hide our process
                {
                    continue;
                }

                //list the loaded runtimes in each process, if the ClrMetaHost APIs are available
                CLRMetaHost mh = null;
                try
                {
                    mh = new CLRMetaHost();
                }
                catch (System.EntryPointNotFoundException)
                {
                    // Intentionally ignore failure to find GetCLRMetaHost().
                    // Downlevel we don't have one.
                    continue;
                }

                IEnumerable<CLRRuntimeInfo> runtimes = null;
                try
                {
                    runtimes = mh.EnumerateLoadedRuntimes(p.Id);
                }
                catch (System.ComponentModel.Win32Exception e)
                {
                    if ((e.NativeErrorCode != 0x0) &&           // The operation completed successfully.
                        (e.NativeErrorCode != 0x3f0) &&         // An attempt was made to reference a token that does not exist.
                        (e.NativeErrorCode != 0x5) &&           // Access is denied.
                        (e.NativeErrorCode != 0x57) &&          // The parameter is incorrect.
                        (e.NativeErrorCode != 0x514) &&         // Not all privileges or groups referenced are assigned to the caller.
                        (e.NativeErrorCode != 0x12))            // There are no more files.
                    {
                        // Unknown/unexpected failures should be reported to the user for diagnosis.
                        Console.WriteLine("Error retrieving loaded runtime information for PID " + p.Id
                            + ", error " + e.ErrorCode + " (" + e.NativeErrorCode + ") '" + e.Message + "'");
                    }

                    // If we failed, don't try to print out any info.
                    if ((e.NativeErrorCode != 0x0) || (runtimes == null))
                    {
                        continue;
                    }
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    if (e.ErrorCode != (int)HResult.E_PARTIAL_COPY)  // Only part of a ReadProcessMemory or WriteProcessMemory request was completed.
                    {
                        // Unknown/unexpected failures should be reported to the user for diagnosis.
                        Console.WriteLine("Error retrieving loaded runtime information for PID " + p.Id
                            + ", error " + e.ErrorCode + "\n" + e.ToString());
                    }

                    continue;
                }

                //if there are no runtimes in the target process, don't print it out
                if (!runtimes.GetEnumerator().MoveNext())
                {
                    continue;
                }

                count++;


                string version = "";
                foreach (CLRRuntimeInfo rti in runtimes)
                {
                    version += rti.GetVersionString();
                }                
                string s = "[" + p.Id + "] [ver=" + version + "] " + p.MainModule.FileName;
                this.listBoxProcesses.Items.Add(new Item(p.Id, s));
            }

            if (count == 0)
            {
                this.listBoxProcesses.Items.Add(new Item(0, "(No active processes)"));
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            m_pid = 0;
            this.Close();
        }

        int m_pid;

        // Get the pid selected from the dialog.
        public int SelectedPid
        {
            get { return m_pid; }
        }


        private void buttonAttach_Click(object sender, EventArgs e)
        {
            object o = this.listBoxProcesses.SelectedItem;
            Item x = (Item)o;
            m_pid = x.Pid;

            this.Close();

        } // end refresh
    }
}