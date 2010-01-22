﻿/*************************************************************************
 *
 * DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 *
 * @Authors:
 *       timop
 *
 * Copyright 2004-2009 by OM International
 *
 * This file is part of OpenPetra.org.
 *
 * OpenPetra.org is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * OpenPetra.org is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
 *
 ************************************************************************/
using System;
using System.Data;
using System.Data.Odbc;
using System.Collections.Specialized;
using Mono.Unix;
using Ict.Petra.Shared;
using Ict.Common;
using Ict.Common.DB;
using Ict.Common.Data;
using Ict.Common.Verification;
using Ict.Common.Printing;
using Ict.Petra.Server.MFinance;
using Ict.Petra.Shared.MFinance;
using Ict.Petra.Shared.MFinance.Gift;
using Ict.Petra.Shared.MFinance.Gift.Data;
using Ict.Petra.Shared.MFinance.GL.Data;
using Ict.Petra.Shared.MFinance.Account.Data;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Shared.MCommon.Data;
using Ict.Petra.Server.MFinance.Account.Data.Access;
using Ict.Petra.Server.MFinance.Gift.Data.Access;
using Ict.Petra.Shared.MPartner;
using Ict.Petra.Server.App.ClientDomain;
using Ict.Petra.Server.MPartner.Partner.Data.Access;
using Ict.Petra.Server.MPartner.Mailroom.Data.Access;
using Ict.Petra.Server.MCommon.Data.Access;

namespace Ict.Petra.Server.MFinance.Gift.WebConnectors
{
    ///<summary>
    /// This connector allows creating the gift receipts
    ///</summary>
    public class TReceiptingWebConnector
    {
        /// <summary>
        /// create the annual gift receipts for all donors in the given year;
        /// returns several html documents, each in its own body tag; for printing with the HTML printer;
        /// TODO images are currently locally linked
        /// </summary>
        public static string CreateAnnualGiftReceipts(Int32 ALedgerNumber, DateTime AStartDate, DateTime AEndDate, string AHTMLTemplate)
        {
            TDBTransaction Transaction = DBAccess.GDBAccessObj.BeginTransaction(IsolationLevel.ReadCommitted);

            // get the local country code
            ALedgerTable ledgerTable = ALedgerAccess.LoadByPrimaryKey(ALedgerNumber, Transaction);
            string LocalCountryCode = ledgerTable[0].CountryCode;

            // first get all donors in the given date range
            string SqlStmt = TDataBase.ReadSqlFile("Gift.ReceiptPrinting.GetDonors.sql");

            OdbcParameter[] parameters = new OdbcParameter[3];
            parameters[0] = new OdbcParameter("LedgerNumber", OdbcType.Int);
            parameters[0].Value = ALedgerNumber;
            parameters[1] = new OdbcParameter("StartDate", OdbcType.Date);
            parameters[1].Value = AStartDate;
            parameters[2] = new OdbcParameter("EndDate", OdbcType.Date);
            parameters[2].Value = AEndDate;

            DataTable donorkeys = DBAccess.GDBAccessObj.SelectDT(SqlStmt, "DonorKeys", Transaction, parameters);

            string ResultDocument = "";

            foreach (DataRow donorrow in donorkeys.Rows)
            {
                Int64 donorKey = Convert.ToInt64(donorrow[0]);
                string donorName = donorrow[1].ToString();

                SqlStmt = TDataBase.ReadSqlFile("Gift.ReceiptPrinting.GetDonationsOfDonor.sql");
                parameters = new OdbcParameter[4];
                parameters[0] = new OdbcParameter("LedgerNumber", OdbcType.Int);
                parameters[0].Value = ALedgerNumber;
                parameters[1] = new OdbcParameter("StartDate", OdbcType.Date);
                parameters[1].Value = AStartDate;
                parameters[2] = new OdbcParameter("EndDate", OdbcType.Date);
                parameters[2].Value = AEndDate;
                parameters[3] = new OdbcParameter("DonorKey", OdbcType.BigInt);
                parameters[3].Value = donorKey;

                DataTable donations = DBAccess.GDBAccessObj.SelectDT(SqlStmt, "Donations", Transaction, parameters);

                if (donations.Rows.Count > 0)
                {
                    string letter = FormatLetter(donorKey, donorName, donations, AHTMLTemplate, LocalCountryCode, Transaction);

                    // TODO: store somewhere that the receipt has been printed?
                    // TODO also store each receipt with the donor in document management, and in contact management?

                    if (ResultDocument.Length > 0)
                    {
                        // ResultDocument += "<div style=\"page-break-before: always;\"/>";
                        string body = letter.Substring(letter.IndexOf("<body"));
                        body = body.Substring(0, body.IndexOf("</html"));
                        ResultDocument += body;
                    }
                    else
                    {
                        // without closing html
                        ResultDocument += letter.Substring(0, letter.IndexOf("</html"));
                    }
                }
            }

            DBAccess.GDBAccessObj.RollbackTransaction();

            return ResultDocument;
        }

        private static string GetStringOrEmpty(object obj)
        {
            if (obj == System.DBNull.Value)
            {
                return "";
            }

            return obj.ToString();
        }

        /// <summary>
        /// format the letter for the donor with all the gifts
        /// </summary>
        private static string FormatLetter(Int64 ADonorKey,
            string ADonorName,
            DataTable ADonations,
            string AHTMLTemplate,
            string ALedgerCountryCode,
            TDBTransaction ATransaction)
        {
            // get details of the donor, and best address

            PLocationTable Location;
            string CountryName;

            if (!GetBestAddress(ADonorKey, out Location, out CountryName, ATransaction))
            {
                return "";
            }

            string msg = AHTMLTemplate;
            msg = msg.Replace("#DONORTITLE", Calculations.FormatShortName(ADonorName, eShortNameFormat.eOnlyTitle));
            msg = msg.Replace("#DONORNAME", Calculations.FormatShortName(ADonorName, eShortNameFormat.eReverseWithoutTitle));
            msg = msg.Replace("#STREETNAME", GetStringOrEmpty(Location[0].StreetName));
            msg = msg.Replace("#LOCATION", GetStringOrEmpty(Location[0].Locality));
            msg = msg.Replace("#ADDRESS3", GetStringOrEmpty(Location[0].Address3));
            msg = msg.Replace("#BUILDING1", GetStringOrEmpty(Location[0].Building1));
            msg = msg.Replace("#BUILDING2", GetStringOrEmpty(Location[0].Building2));
            msg = msg.Replace("#CITY", GetStringOrEmpty(Location[0].City));
            msg = msg.Replace("#POSTALCODE", GetStringOrEmpty(Location[0].PostalCode));
            msg = msg.Replace("#DATE", DateTime.Now.ToString("d. MMMM yyyy"));

            // according to German Post, there is no country code in front of the post code
            // if country code is same for the address of the recipient and this office, then COUNTRYNAME is cleared
            if (GetStringOrEmpty(Location[0].CountryCode) != ALedgerCountryCode)
            {
                msg = msg.Replace("#COUNTRYNAME", CountryName);
            }
            else
            {
                msg = msg.Replace("#COUNTRYNAME", "");
            }

            // recognise detail lines automatically
            string RowTemplate;
            msg = TPrinterHtml.GetTableRow(msg, "#AMOUNT", out RowTemplate);
            string rowTexts = "";

            foreach (DataRow rowGifts in ADonations.Rows)
            {
                DateTime dateEffective = Convert.ToDateTime(rowGifts["DateEffective"]);
                Double amount = Convert.ToDouble(rowGifts["Amount"]);
                string commentOne = rowGifts["CommentOne"].ToString();
                string accountDesc = rowGifts["AccountDesc"].ToString();
                string costcentreDesc = rowGifts["CostCentreDesc"].ToString();

                rowTexts += RowTemplate.
                            Replace("#DONATIONDATE", dateEffective.ToShortDateString()).
                            Replace("#AMOUNT", String.Format("{0:C}", amount)).
                            Replace("#COMMENTONE", commentOne).
                            Replace("#ACCOUNTDESC", accountDesc).
                            Replace("#COSTCENTREDESC", costcentreDesc);
            }

            return msg.Replace("#ROWTEMPLATE", rowTexts);
        }

        private static bool GetBestAddress(Int64 APartnerKey, out PLocationTable AAddress, out string ACountryNameLocal, TDBTransaction ATransaction)
        {
            AAddress = null;
            ACountryNameLocal = "";

            DataSet PartnerLocationsDS = new DataSet();

            PartnerLocationsDS.Tables.Add(new PPartnerLocationTable());
            PartnerLocationsDS.Tables.Add(new PCountryTable());
            DataTable PartnerLocationTable = PartnerLocationsDS.Tables[PPartnerLocationTable.GetTableName()];
            PCountryTable CountryTable = (PCountryTable)PartnerLocationsDS.Tables[PCountryTable.GetTableName()];
            CountryTable.DefaultView.Sort = PCountryTable.GetCountryCodeDBName();

            // add special column BestAddress and Icon
            PartnerLocationTable.Columns.Add(new System.Data.DataColumn("BestAddress", typeof(Boolean)));
            PartnerLocationTable.Columns.Add(new System.Data.DataColumn("Icon", typeof(Int32)));

            // find all locations of the partner, put it into a dataset
            PPartnerLocationAccess.LoadViaPPartner(PartnerLocationsDS, APartnerKey, ATransaction);

            Ict.Petra.Shared.MPartner.Calculations.DeterminePartnerLocationsDateStatus(PartnerLocationsDS);
            Ict.Petra.Shared.MPartner.Calculations.DetermineBestAddress(PartnerLocationsDS);

            foreach (PPartnerLocationRow row in PartnerLocationTable.Rows)
            {
                // find the row with BestAddress = 1
                if (Convert.ToInt32(row["BestAddress"]) == 1)
                {
                    // we also want the post address, need to load the p_location table:
                    AAddress = PLocationAccess.LoadByPrimaryKey(row.SiteKey, row.LocationKey, ATransaction);

                    if (CountryTable.DefaultView.Find(AAddress[0].CountryCode) == -1)
                    {
                        CountryTable.Merge(PCountryAccess.LoadByPrimaryKey(AAddress[0].CountryCode, ATransaction));
                    }

                    ACountryNameLocal = CountryTable[CountryTable.DefaultView.Find(AAddress[0].CountryCode)].CountryNameLocal;
                }
            }

            return AAddress != null;
        }
    }
}