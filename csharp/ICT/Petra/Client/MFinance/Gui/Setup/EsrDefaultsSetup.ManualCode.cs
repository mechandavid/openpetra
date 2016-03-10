﻿// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       Tim Ingham
//
// Copyright 2015 by OM International
//
// This file is part of OpenPetra.org.
//
// OpenPetra.org is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra.org is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
//

using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Client.MFinance.Logic;
using SourceGrid;
using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

namespace Ict.Petra.Client.MFinance.Gui.Setup.Gift
{
    public partial class TFrmEsrDefaultsSetup
    {
        private DataTable FesrDefaults;
        DataRow FselectedRow = null;
        Int32 FLedgerNumber = 0;
        Boolean FsuppressChangeEvent = false;

        private void InitializeManualCode()
        {
            FesrDefaults = TRemote.MFinance.Gift.WebConnectors.GetEsrDefaults();
            grdDetails.Columns.Add("a_partner_key_n", "Partner Key", typeof(Int64));
            grdDetails.Columns.Add("a_new_partner_key_n", "Replacement", typeof(Int64));
            grdDetails.Columns.Add("a_motiv_group_s", "Motiv. Group", typeof(String));
            grdDetails.Columns.Add("a_motiv_detail_s", "Motiv. Detail", typeof(String));

            grdDetails.Selection.SelectionChanged += Selection_SelectionChanged;
            grdDetails.Selection.FocusRowLeaving += UpdateGrid;
            txtPartnerKey.Leave += initNewPartnerKey;
            txtNewPartnerKey.Leave += UpdateGrid;
            cmbMotivGroup.SelectedValueChanged += UpdateMotivationDetail;
            cmbMotivDetail.SelectedValueChanged += UpdateGrid;

            DataView myDataView = FesrDefaults.DefaultView;
            myDataView.Sort = "a_partner_key_n";
            myDataView.AllowNew = false;
            grdDetails.DataSource = new DevAge.ComponentModel.BoundDataView(myDataView);
        }

        void UpdateGrid(object sender, EventArgs e)
        {
            if (!FsuppressChangeEvent)
            {
                GetDataFromControls();
            }
        }

        void initNewPartnerKey(object sender, EventArgs e)
        {
            String PartnerKeySt = txtPartnerKey.Text;

            if (PartnerKeySt == "")
            {
/*
 *              MessageBox.Show("Error: Please enter a PartnerKey.",
 *                  "ESR Defaults", MessageBoxButtons.OK, MessageBoxIcon.Error);
 *              txtPartnerKey.Text = FselectedRow["a_partner_key_n"].ToString();
 *              txtPartnerKey.Focus();
 */
                return;
            }

            Int64 PartnerKey;
            Boolean IsNumeric = Int64.TryParse(PartnerKeySt, out PartnerKey);

            if (!IsNumeric)
            {
                MessageBox.Show("Error: Please check your entry for PartnerKey.",
                    "ESR Defaults", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPartnerKey.Text = FselectedRow["a_partner_key_n"].ToString();
                txtPartnerKey.Focus();
                return;
            }

            if (FesrDefaults.DefaultView.Find(PartnerKeySt) > 0)
            {
                MessageBox.Show(String.Format("Error: An entry already exists for partner key {0}.", PartnerKeySt),
                    "ESR Defaults", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPartnerKey.Text = FselectedRow["a_partner_key_n"].ToString();
                txtPartnerKey.Focus();
            }

            if (txtNewPartnerKey.Text == "0000000000")
            {
                txtNewPartnerKey.Text = txtPartnerKey.Text;
            }

            UpdateGrid(sender, e);
        }

        void UpdateMotivationDetail(object sender, EventArgs e)
        {
            String motivationGroup = cmbMotivGroup.GetSelectedString();

            TFinanceControls.ChangeFilterMotivationDetailList(ref cmbMotivDetail, motivationGroup);
            UpdateGrid(sender, e);
        }

        /// <summary>
        /// Only show motivation details for this Ledger:
        /// </summary>
        public Int32 LedgerNumber
        {
            set
            {
                FLedgerNumber = value;
                TFinanceControls.InitialiseMotivationGroupList(ref cmbMotivGroup, FLedgerNumber, true);
                TFinanceControls.InitialiseMotivationDetailList(ref cmbMotivDetail, FLedgerNumber, true);
                grdDetails.SelectRowInGrid(1, true);
            }
        }

        private void Selection_SelectionChanged(object sender, RangeRegionChangedEventArgs e)
        {
            int gridRow = grdDetails.Selection.ActivePosition.Row;

            ShowGridRow(gridRow);
        }

        private void GetDataFromControlsManual()
        {
            if (FselectedRow != null)
            {
                Int64 partnerKey;

                if (Int64.TryParse(txtPartnerKey.Text, out partnerKey))
                {
                    FselectedRow["a_partner_key_n"] = partnerKey;
                    Int64 newPartnerKey = partnerKey;

                    if (Int64.TryParse(txtNewPartnerKey.Text, out newPartnerKey))
                    {
                        FselectedRow["a_new_partner_key_n"] = newPartnerKey;
                    }

                    FselectedRow["a_motiv_group_s"] = cmbMotivGroup.GetSelectedString();
                    FselectedRow["a_motiv_detail_s"] = cmbMotivDetail.GetSelectedString();
                }
                else
                {
                    MessageBox.Show("Error: PartnerKey is empty. Please delete unused row.",
                        "ESR Defaults", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPartnerKey.Text = FselectedRow["a_partner_key_n"].ToString();
                }
            }
        }

        private void ShowGridRow(Int32 ARowNumberInGrid)
        {
            DataRowView rowView = (DataRowView)grdDetails.Rows.IndexToDataSourceRow(ARowNumberInGrid);

            if (rowView != null)
            {
                FselectedRow = rowView.Row;
                ShowDataRow(FselectedRow);
            }
        }

        private void ShowDataRow(DataRow Row)
        {
            FsuppressChangeEvent = true;
            txtPartnerKey.Text = Row["a_partner_key_n"].ToString();
            txtNewPartnerKey.Text = Row["a_new_partner_key_n"].ToString();
            cmbMotivGroup.SetSelectedString(Row["a_motiv_group_s"].ToString());
            cmbMotivDetail.SetSelectedString(Row["a_motiv_detail_s"].ToString());
            FsuppressChangeEvent = false;
        }

        private void NewRecord(Object sender, EventArgs e)
        {
            GetDataFromControls();
            DataRow newRow = FesrDefaults.NewRow();
            newRow["a_motiv_group_s"] = "GIFT";
            FesrDefaults.Rows.Add(newRow);
            grdDetails.SelectRowInGrid(1, true);
            txtPartnerKey.Focus();

            FPetraUtilsObject.SetChangedFlag();
        }

        private void DeleteRecord(Object sender, EventArgs e)
        {
            int gridRow = grdDetails.Selection.ActivePosition.Row;

            FselectedRow.Delete();
            grdDetails.SelectRowInGrid(gridRow, true);
            FPetraUtilsObject.SetChangedFlag();
        }

        private void FileSave(Object sender, EventArgs e)
        {
            SaveChanges();
        }

        /// <summary>
        /// Called from PetraEditForm
        /// </summary>
        /// <param name="Msg"></param>
        /// <returns></returns>
        public Int32 GetChangedRecordCount(out String Msg)
        {
            Msg = "";
            DataTable changes = FesrDefaults.GetChanges();
            return changes.Rows.Count;
        }

        /// <summary>
        /// Called from PetraEditForm
        /// </summary>
        /// <returns></returns>
        public bool SaveChanges()
        {
            Boolean Res = TRemote.MFinance.Gift.WebConnectors.CommitEsrDefaults(FesrDefaults);

            if (Res)
            {
                FPetraUtilsObject.DisableSaveButton();
            }

            return Res;
        }
    }
}