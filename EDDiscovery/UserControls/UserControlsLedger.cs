﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EDDiscovery.Controls;
using EDDiscovery2.DB;

namespace EDDiscovery.UserControls
{
    public partial class UserControlsLedger : UserControlCommonBase
    {
        private int displaynumber = 0;
        EDDiscoveryForm discoveryform;

        EventFilterSelector cfs = new EventFilterSelector();

        private string DbFilterSave { get { return "LedgerGridEventFilter" + ((displaynumber > 0) ? displaynumber.ToString() : ""); } }
        private string DbColumnSave { get { return ("LedgerGrid") + ((displaynumber > 0) ? displaynumber.ToString() : "") + "DGVCol"; } }
        private string DbHistorySave { get { return "LedgerGridEDUIHistory" + ((displaynumber > 0) ? displaynumber.ToString() : ""); } }

        #region Init

        public UserControlsLedger()
        {
            InitializeComponent();
        }

        public void Init(EDDiscoveryForm f , int vn) //0=primary, 1 = first windowed version, etc
        {
            discoveryform = f;
            displaynumber = vn;

            dataGridViewLedger.MakeDoubleBuffered();
            dataGridViewLedger.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dataGridViewLedger.RowTemplate.Height = 26;

            cfs.ConfigureThirdOption("Cash Transactions", string.Join(";", EliteDangerous.JournalEntry.GetListOfEventsWithOptMethod(true, "Ledger")));

            cfs.Changed += EventFilterChanged;
            TravelHistoryFilter.InitaliseComboBox(comboBoxHistoryWindow, DbHistorySave);
        }

        #endregion

        #region Display

        MaterialCommoditiesLedger current_mc;

        public void Display(MaterialCommoditiesLedger mc)
        {
            dataGridViewLedger.Rows.Clear();
            bool utctime = EDDiscoveryForm.EDDConfig.DisplayUTC;

            current_mc = mc;
            
            labelNoItems.Visible = true;

            if (mc != null && mc.Transactions.Count > 0)
            {
                var filter = (TravelHistoryFilter)comboBoxHistoryWindow.SelectedItem ?? TravelHistoryFilter.NoFilter;
                List<MaterialCommoditiesLedger.Transaction> filteredlist = filter.Filter(mc.Transactions);

                filteredlist = FilterByJournalEvent(filteredlist, DB.SQLiteDBClass.GetSettingString(DbFilterSave, "All"));

                if (filteredlist.Count > 0)
                {
                    for (int i = filteredlist.Count - 1; i >= 0; i--)
                    {
                        MaterialCommoditiesLedger.Transaction tx = filteredlist[i];

                        object[] rowobj = { utctime ? tx.utctime : tx.utctime.ToLocalTime() ,
                                            Tools.SplitCapsWord(tx.jtype.ToString()),
                                            tx.notes,
                                            (tx.cashadjust>0) ? tx.cashadjust.ToString("N0") : "",
                                            (tx.cashadjust<0) ? (-tx.cashadjust).ToString("N0") : "",
                                            tx.cash.ToString("N0"),
                                            (tx.profitperunit!=0) ? tx.profitperunit.ToString("N0") : ""
                                        };

                        dataGridViewLedger.Rows.Add(rowobj);
                    }

                    StaticFilters.FilterGridView(dataGridViewLedger, textBoxFilter.Text);

                    labelNoItems.Visible = false;
                }
            }

            dataGridViewLedger.Columns[0].HeaderText = utctime ? "Game Time" : "Time";
        }

        public List<MaterialCommoditiesLedger.Transaction> FilterByJournalEvent(List<MaterialCommoditiesLedger.Transaction> txlist, string eventstring)
        {
            if (eventstring.Equals("All"))
                return txlist;
            else
            {
                string[] events = eventstring.Split(';');
                return (from tx in txlist where tx.IsJournalEventInEventFilter(events) select tx).ToList();
            }
        }



        #endregion

        #region Layout

        public override void LoadLayout()
        {
            DGVLoadColumnLayout(dataGridViewLedger, DbColumnSave);
        }

        public override void SaveLayout()
        {
            DGVSaveColumnLayout(dataGridViewLedger, DbColumnSave);
        }

        public override int ColumnWidthPreference(DataGridView notused, int i)  // DGV is passed back in case we have more than one.. we dont
        {
            int[] pref = new int[] { 2,6,0,1,3,4,5 };
            return (i < pref.Length) ? pref[i] : -1;
        }

        public override int ColumnExpandPreference() { return 2; }

        private void dataGridViewMC_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            DGVColumnWidthChanged(dataGridViewLedger);
        }

        private void dataGridViewMC_Resize(object sender, EventArgs e)
        {
            DGVResize(dataGridViewLedger);
        }

        #endregion

        private void checkBoxCustomCashOnly_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void buttonFilter_Click(object sender, EventArgs e)
        {
            Button b = sender as Button;
            cfs.FilterButton(DbFilterSave, b,
                             discoveryform.theme.TextBackColor, discoveryform.theme.TextBlockColor, this.FindForm() ,
                             EliteDangerous.JournalEntry.GetListOfEventsWithOptMethod(true, "Ledger", "LedgerNC")
                             );
        }

        private void comboBoxHistoryWindow_SelectedIndexChanged(object sender, EventArgs e)
        {
            DB.SQLiteDBClass.PutSettingInt(DbHistorySave, comboBoxHistoryWindow.SelectedIndex);

            if (current_mc != null)
            {
                Display(current_mc);
            }
        }

        private void textBoxFilter_KeyUp(object sender, KeyEventArgs e)
        {
            StaticFilters.FilterGridView(dataGridViewLedger, textBoxFilter.Text);
        }

        private void EventFilterChanged(object sender, EventArgs e)
        {
            Display(current_mc);
        }

    }
}
