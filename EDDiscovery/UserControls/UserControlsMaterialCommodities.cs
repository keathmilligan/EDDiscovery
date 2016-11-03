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
    public partial class UserControlsMaterialCommodities : UserControlCommonBase
    {
        public bool materials = false;
        private int displaynumber = 0;
        private int namecol, abvcol, catcol, typecol, numcol, pricecol;
        List<MaterialCommodities> last_mc = null;

        public delegate void ChangedCount(List<MaterialCommodities> ls);
        public event ChangedCount OnChangedCount;

        public delegate void RequestRefresh();
        public event RequestRefresh OnRequestRefresh;

        private string DbColumnSave { get { return ((materials) ? "MaterialsGrid" : "CommoditiesGrid") + ((displaynumber > 0) ? displaynumber.ToString() : "") + "DGVCol"; } }

        #region Init

        public UserControlsMaterialCommodities()
        {
            InitializeComponent();
        }

        public void Init(int vn) //0=primary, 1 = first windowed version, etc
        {
            displaynumber = vn;

            dataGridViewMC.MakeDoubleBuffered();
            dataGridViewMC.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dataGridViewMC.RowTemplate.Height = 26;

            if (materials)
                dataGridViewMC.Columns.Remove(dataGridViewMC.Columns[5]);       // to give name,shortname abv,category,type,number
            else
            {
                dataGridViewMC.Columns.Remove(dataGridViewMC.Columns[1]);       //shortname
                dataGridViewMC.Columns.Remove(dataGridViewMC.Columns[1]);       //then category to give name,type,number, avg price
            }

            namecol = 0;
            abvcol = (materials) ? 1 : -1;
            catcol = (materials) ? 2 : -1;
            typecol = (materials) ? 3 : 1;
            numcol = (materials) ? 4 : 2;
            pricecol = (materials) ? -1 : 3;
        }

        #endregion

        #region Display


        public void Display(List<MaterialCommodities> mc)
        {
            DisableEditing();

            last_mc = mc;

            dataGridViewMC.Rows.Clear();

            if (mc != null && mc.Count > 0)
            {
                labelNoItems.Visible = false;

                foreach (MaterialCommodities m in mc)
                {
                    if (materials)
                    {
                        object[] rowobj = { m.name, m.shortname, m.category, m.type, m.count.ToString() };
                        dataGridViewMC.Rows.Add(rowobj);
                    }
                    else
                    {
                        object[] rowobj = { m.name, m.type, m.count.ToString(), m.price.ToString("0.#") };
                        dataGridViewMC.Rows.Add(rowobj);

                    }
                }
            }
            else
            {
                labelNoItems.Visible = true;
            }
        }

        #endregion

        #region Layout

        public override void LoadLayout()
        {
            DGVLoadColumnLayout(dataGridViewMC, DbColumnSave);
        }

        public override void SaveLayout()
        {
            DGVSaveColumnLayout(dataGridViewMC, DbColumnSave);
        }

        public override int ColumnWidthPreference(DataGridView notused, int i)  // DGV is passed back in case we have more than one.. we dont
        {
            if (materials)
            {
                int[] pref = new int[] { 0, 2, 3, 4, 1 };
                return (i < pref.Length) ? pref[i] : -1;
            }
            else
            {
                int[] pref = new int[] { 0, 1, 2, 3 };
                return (i < pref.Length) ? pref[i] : -1;
            }
        }

        public override int ColumnExpandPreference() { return 0; }


        private void dataGridViewMC_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            DGVColumnWidthChanged(dataGridViewMC);
        }

        private void dataGridViewMC_Resize(object sender, EventArgs e)
        {
            DGVResize(dataGridViewMC);
        }

        #endregion

        bool ignoresi = false;
        private void comboBoxCustomAdd_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ignoresi)
                return;

            int i = comboBoxCustomAdd.SelectedIndex;

            if (i < mclist.Count)
            {
                MaterialCommodities mc = mclist[i];

                int j = 0;

                for (; j < dataGridViewMC.Rows.Count; j++)
                {
                    if (((string)(dataGridViewMC.Rows[j].Cells[namecol].Value)).Equals(mc.name))
                        break;
                }

                if (j == dataGridViewMC.Rows.Count)
                {
                    if (materials)
                    {
                        object[] rowobj = { mc.name, mc.shortname, mc.category, mc.type, "0" };
                        dataGridViewMC.Rows.Add(rowobj);
                    }
                    else
                    {
                        object[] rowobj = { mc.name, mc.type, "0", "0" };
                        dataGridViewMC.Rows.Add(rowobj);
                    }

                    labelNoItems.Visible = false;
                }
            }
            else
            {
                dataGridViewMC.Rows.Add();
                DataGridViewRow rw = dataGridViewMC.Rows[dataGridViewMC.Rows.Count - 1];
                rw.Cells[typecol].ReadOnly = true;
                if (materials)
                    rw.Cells[abvcol].ReadOnly = true;

                labelNoItems.Visible = false;
            }

            ignoresi = true;
            comboBoxCustomAdd.SelectedIndex = -1;
            comboBoxCustomAdd.Text = "Select another";
            ignoresi = false;
        }

        List<MaterialCommodities> mclist = new List<MaterialCommodities>();

        private void buttonExtModify_Click(object sender, EventArgs e)
        {
            if (buttonExtApply.Enabled)     // then its cancel
            {
                DisableEditing();
                Display(last_mc);
            }
            else
            {
                dataGridViewMC.Columns[0].ReadOnly = dataGridViewMC.Columns[1].ReadOnly =
                    dataGridViewMC.Columns[3].ReadOnly = dataGridViewMC.Columns[2].ReadOnly = false;
                if (materials)
                    dataGridViewMC.Columns[4].ReadOnly = false;

                buttonExtApply.Enabled = true;
                comboBoxCustomAdd.Enabled = true;
                buttonExtModify.Text = "Cancel";

                ResetCombo();
            }
        }

        private void ResetCombo()
        {
            List<MaterialCommodities> list = MaterialCommodities.GetMaterialsCommoditiesList;
            comboBoxCustomAdd.Items.Clear();
            mclist.Clear();
            foreach (MaterialCommodities mc in list)
            {
                if (!mc.category.Equals(MaterialCommodities.CommodityCategory) == materials)
                {
                    mclist.Add(mc);
                    comboBoxCustomAdd.Items.Add(mc.name);
                }
            }

            comboBoxCustomAdd.Items.Add("User defined");
        }

        private void DisableEditing()
        {
            if (buttonExtApply.Enabled)
            {
                foreach (DataGridViewColumn c in dataGridViewMC.Columns)
                    c.ReadOnly = true;

                comboBoxCustomAdd.Enabled = false;
                buttonExtApply.Enabled = false;
                buttonExtModify.Text = "Modify";
            }
        }

        private void buttonExtApply_Click(object sender, EventArgs e)
        {
            List<MaterialCommodities> mcchange = new List<MaterialCommodities>();

            bool updateddb = false;

            for (int i = 0; i < last_mc.Count; i++)
            {
                string name = (string)dataGridViewMC.Rows[i].Cells[namecol].Value;
                string abv = (abvcol >= 0) ? (string)dataGridViewMC.Rows[i].Cells[abvcol].Value : last_mc[i].shortname;
                string cat = (catcol >= 0) ? (string)dataGridViewMC.Rows[i].Cells[catcol].Value : last_mc[i].category;
                string type = (string)dataGridViewMC.Rows[i].Cells[typecol].Value;

                if (!last_mc[i].name.Equals(name) || !last_mc[i].shortname.Equals(abv) || !last_mc[i].category.Equals(cat)
                    || !last_mc[i].type.Equals(type))
                {
                    //System.Diagnostics.Debug.WriteLine("Row " + i + " changed text");
                    MaterialCommodities.ChangeDbText(last_mc[i].fdname, name, abv, cat, type);
                    updateddb = true;
                }

                int numvalue = 0;
                int.TryParse((string)dataGridViewMC.Rows[i].Cells[numcol].Value, out numvalue);

                double price = 0;
                bool pricechange = false;

                if (!materials)
                {
                    double.TryParse((string)dataGridViewMC.Rows[i].Cells[pricecol].Value, out price);
                    pricechange = Math.Abs(last_mc[i].price - price) >= 0.01;
                }

                if (last_mc[i].count != numvalue || (materials || pricechange))
                {
                    mcchange.Add(new MaterialCommodities(0, last_mc[i].category, last_mc[i].name, last_mc[i].fdname, "", "", Color.Red, 0, numvalue, (pricechange) ? price : 0));
                    //System.Diagnostics.Debug.WriteLine("Row " + i + " changed number");
                }
            }

            for (int i = last_mc.Count; i < dataGridViewMC.Rows.Count; i++)                // these have been added
            {
                string name = (string)dataGridViewMC.Rows[i].Cells[namecol].Value;
                string cat = (materials) ? (string)dataGridViewMC.Rows[i].Cells[catcol].Value : MaterialCommodities.CommodityCategory;
                string fdname = Tools.FDName(name);

                int numvalue = 0;
                if (int.TryParse((string)dataGridViewMC.Rows[i].Cells[numcol].Value, out numvalue) && cat.Length > 0 && name.Length > 0)
                {
                    mcchange.Add(new MaterialCommodities(0, cat, name, fdname, "", "", Color.Red, 0, numvalue));
                }
            }

            DisableEditing();

            if (OnChangedCount != null)
                OnChangedCount(mcchange);
            else if ( updateddb && OnRequestRefresh != null)
                OnRequestRefresh();
        }

        private void dataGridViewMC_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("End edit");
            DataGridViewCell c = dataGridViewMC.Rows[e.RowIndex].Cells[e.ColumnIndex];

            if (e.ColumnIndex == numcol)
            {
                int originalvalue = (e.RowIndex < last_mc.Count) ? last_mc[e.RowIndex].count : 0;
                int newvalue = 0;
                if (!int.TryParse((string)c.Value, out newvalue) && newvalue >= 0)
                    c.Value = originalvalue.ToString();
            }
        }
    }

    public class UserControlsMaterials : UserControlsMaterialCommodities
    {
        public UserControlsMaterials()
        {
            materials = true;
        }
    }

    public class UserControlsCommodities : UserControlsMaterialCommodities
    {
        public UserControlsCommodities()
        {
            materials = false;
        }
    }
}
