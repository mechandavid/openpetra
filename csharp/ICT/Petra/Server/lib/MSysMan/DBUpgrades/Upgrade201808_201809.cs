//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//
// Copyright 2004-2018 by OM International
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
using System;
using System.Data;
using Ict.Common.DB;
using Ict.Common;
using Ict.Common.Data;

namespace Ict.Petra.Server.MSysMan.DBUpgrades
{
    /// <summary>
    /// Upgrade the database
    /// </summary>
    public static partial class TDBUpgrade
    {
        /// Upgrade to version 2018-09
        public static bool UpgradeDatabase201808_201809()
        {
            // there are some changes to the database structure
            TDataBase db = DBAccess.Connect("TDBUpgrade");
            TDBTransaction SubmitChangesTransaction = new TDBTransaction();
            bool SubmitOK = false;

            db.WriteTransaction(ref SubmitChangesTransaction,
                ref SubmitOK,
                delegate
                {
                    string[] SqlStmts = TDataBase.ReadSqlFile("Upgrade201808_201809.sql").Split(new char[]{';'});

                    foreach (string stmt in SqlStmts)
                    {
                        if (stmt.Trim().Length > 0)
                        {
                            db.ExecuteNonQuery(stmt, SubmitChangesTransaction);
                        }
                    }

                    SubmitOK = true;
                });
            return true;
        }
    }
}
