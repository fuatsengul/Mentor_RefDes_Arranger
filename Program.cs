using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using MGCPCB;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using MGCPCB;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace xPCB_RefDesArranger
{
    class Program
    {
        static MGCPCB.Document pcbDoc;
        static dynamic FindOrAddLayer(string LayerName, dynamic _doc)
        {
            dynamic _lay = _doc.FindUserLayer(LayerName);
            if (_lay == null)
            {
                _lay = _doc.SetupParameter.PutUserLayer(LayerName);
            }
            return _lay;
        }

        [STAThread]
        static void Main(string[] args)
        {
            #region Instance Connection Code
            try
            {
                var clsid = Marshal.GetTypeFromCLSID(
                            new Guid("44983CB8-19B0-4695-937A-6FF0B74ECFC5")
                        );
                dynamic _server = Activator.CreateInstance(clsid);


                _server.SetEnvironment("");
                string VxVersion = _server.sddVersion;
                string strSDD_HOME = _server.sddHome;
                int length = strSDD_HOME.IndexOf("SDD_HOME");
                strSDD_HOME = strSDD_HOME.Substring(0, length).Replace("\\", "\\\\") + "SDD_HOME";
                _server.SetEnvironment(strSDD_HOME);
                string progID = _server.ProgIDVersion;

                object[,] _releases = (object[,])_server.GetInstalledReleases();
                dynamic pcbApp = null;

                for (int i = 1; i < _releases.Length / 4; i++)
                {
                    string _com_version = Convert.ToString(_releases[i, 0]);
                    try
                    {
                        pcbApp = Interaction.GetObject(null, "MGCPCB.Application." + _com_version);

                        pcbDoc = pcbApp.ActiveDocument;
                        dynamic licApp = Interaction.CreateObject("MGCPCBAutomationLicensing.Application." + _com_version);
                        int _token = licApp.GetToken(pcbDoc.Validate(0));
                        pcbDoc.Validate(_token);

                        break;
                    }
                    catch (Exception m)
                    {
                    }
                }


                if (pcbApp == null)
                {
                    System.Windows.Forms.MessageBox.Show("Could not found active Xpedition or PADSPro Application");
                    System.Environment.Exit(1);
                }

                

            }
            catch (Exception m)
            {
                MessageBox.Show(m.Message + "\r\n" + m.Source + "\r\n" + m.StackTrace);
            }
            #endregion

            #region Work Code
            MGCSDDOUTPUTWINDOWLib.MGCSDDOutputLogControl msgWnd = null;
            MGCSDDOUTPUTWINDOWLib.HtmlCtrl _tabCtrl = null;

            foreach (dynamic addin in (dynamic)pcbDoc.Application.Addins)
            {
                if (addin.Name == "Message Window")
                {
                    Console.WriteLine(addin.Control);
                    addin.Visible = true;
                    msgWnd = addin.Control;
                }
            }

            if (msgWnd != null)
            {
                _tabCtrl = msgWnd.AddTab("Ref-Des Arranger");
                _tabCtrl.Clear();
                _tabCtrl.Activate();
            }

            var addText = new Action<string>(text =>
            {
                if (_tabCtrl != null)
                {
                    _tabCtrl.AppendText(text + "\r\n");
                }
            });

            var addHtml = new Action<string>(html =>
            {
                if (_tabCtrl != null)
                {
                    _tabCtrl.AppendHTML(html);
                }
            });



            addText("*THIS CODE IS A OPEN-SOURCE SOFTWARE UNDER MIT LICENSE");
            addText("*PROVIDED \"AS IS\" WITHOUT WARRANTY OF ANY KIND,");
            addText("*EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED");
            addText("*WARRANTIES OF MERCHANTABILITY AND / OR FITNESS FOR A PARTICULAR PURPOSE.");
            addText("");
            addHtml("<p style=\"{color: red; font-weight: bold;}\">Copyright 2008-2020; Milbitt Engineering. All rights reserved.<br />" +
                    "<a href=\"https://www.milbitt.com\">www.milbitt.com</a><br />" +
                    "<a href=\"mailto:info@milbitt.com\">info@milbitt.com</a></p>");
            // Everything is OK, then....
            System.Threading.Thread.Sleep(2000);

            MGCPCB.Components _comps = pcbDoc.get_Components(EPcbSelectionType.epcbSelectSelected);
            if (_comps.Count == 0)
            {
                pcbDoc.Application.Gui.StatusBarText("Select some components", EPcbStatusField.epcbStatusFieldError);
                addText("Error: Select some components");
                return;
            }

            pcbDoc.Application.Gui.ProgressBarInitialize(false, "", 0, 0);
            pcbDoc.Application.Gui.ProgressBarInitialize(true, "Starting Ref-Des Arranger", 100, 0);
            addText("Starting Ref-Des Arranger");

            try
            {
                pcbDoc.TransactionStart(EPcbDRCMode.epcbDRCModeNone);
                CellFixEngine _cfe = new CellFixEngine();
                Parallel.ForEach(pcbDoc.get_Components(EPcbSelectionType.epcbSelectSelected, EPcbComponentType.epcbCompAll, EPcbCelltype.epcbCelltypeAll).OfType<MGCPCB.Component>(),
                    comp => {
                        addText(string.Format("Processing Part: {0}({1})", comp.RefDes, comp.CellName));
                        _cfe.BatchFixComponent(ref comp);
                    }
                );
            }
            catch (Exception m)
            {
                addHtml("<p style=\"{color: red; font-weight: bold;}\">Error! " + m.Message + "<br/>" + m.Source + "<br/>" + m.StackTrace + "</p>");
            }
            finally
            {
                pcbDoc.TransactionEnd();
                pcbDoc.Application.Gui.ProgressBar(100);
                pcbDoc.Application.Gui.ProgressBarInitialize(false, "Ref-Des Arranger: Completed");
                addHtml("<p style=\"{color: #006600; font-weight: bold;}\">Operation Competed</p>");
                addText("");
                addHtml("<p style=\"{color: red; font-weight: bold;}\">Copyright 2008-2020; Milbitt Engineering. All rights reserved.<br />" +
                    "<a href=\"www.milbitt.com\">www.milbitt.com</a></p>");
                addText("");
            }
            #endregion 
        }
    }
}
