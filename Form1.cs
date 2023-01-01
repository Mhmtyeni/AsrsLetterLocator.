using Guna.UI2.WinForms;
using Microsoft.VisualBasic;
using S7.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AsrsLetterLocator
{
    public partial class Form1 : Form
    {
        // =========================
        SqlConnection sqlConnection;
        SqlDataAdapter dataAdapter;
        DataTable dataTable;
        // =========================
        public PcToPlc pcToPlc = new PcToPlc();
        public PlcToPc plcToPc = new PlcToPc();
        Point pbXpoint = new Point();
        Point pbZpoint = new Point();
        // =========================
        public static DropProductRequestProcess dropProductProcess = DropProductRequestProcess.Initialize;
        public static GetProductRequestProcess getProductProcess = GetProductRequestProcess.Initialize;
        // =========================
        public Plc myPlc;
        public Thread thread;
        public List<int> shelves = new List<int> { 3, 7, 11, 15, 19, 23 };
        // =========================
        bool bIsConnected = false;
        bool bProcessExist = false;
        // =========================
        public Product asrsProduct = new Product();
        // =========================
        [Obsolete]
        public Form1()
        {
            InitializeComponent();
        }
        #region ' Buttons '
        // =======================================
        [Obsolete]
        private void btnConnectPlc_Click(object sender, EventArgs e)
        {
            try
            {
                if (!bIsConnected)
                {
                    myPlc.Open();
                    pcToPlc.bPcActive = true;
                    myPlc.WriteClass(pcToPlc, 8);
                    Thread.Sleep(100);
                    myPlc.ReadClass(plcToPc, 9);
                    if (myPlc.IsConnected && plcToPc.bPlcActive)
                    {
                        pokeMessage("Bağlantı başarılı!", MessageDialogIcon.Information, MessageDialogButtons.OK);
                        btnConnectPlc.Text = "Bağlantıyı Kes";
                        if (thread.ThreadState != ThreadState.Suspended)
                            thread.Start();
                        else
                            thread.Resume();
                        bIsConnected = true;
                    }
                    else
                        pokeMessage("Asrs ulaşılamıyor!", MessageDialogIcon.Error, MessageDialogButtons.OK);
                }
                else
                {
                    thread.Suspend();
                    pcToPlc.bPcActive = false;
                    myPlc.WriteClass(pcToPlc, 8);
                    myPlc.Close();
                    pokeMessage("Bağlantı kesildi!", MessageDialogIcon.Information, MessageDialogButtons.OK);
                    btnConnectPlc.Text = "Bağlan";
                    if (!myPlc.IsConnected)
                        bIsConnected = false;
                }
            }
            catch (Exception ex)
            {
                pokeMessage(ex.Message, MessageDialogIcon.Error, MessageDialogButtons.OK);
            }
        }
        // =======================================
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (myPlc.IsConnected)
            {
                if (!bProcessExist)
                {
                    Product tempProduct = new Product();
                    Shelf tempShelf = new Shelf();
                    Shelf dropShelf = new Shelf();
                    int num = 0;
                    List<Shelf> tempList = new List<Shelf>();
                    try
                    {
                        string sProductName = Interaction.InputBox("Koyduğunuz harfi giriniz!", "ASRS Uygulaması");
                        if (sProductName != "")
                        {
                            // ================================
                            if (sqlConnection.State != ConnectionState.Open)
                                sqlConnection.Open();
                            dataAdapter = new SqlDataAdapter("Select * from Product where Name ='" + sProductName + "'", sqlConnection);
                            dataTable = new DataTable();
                            dataAdapter.Fill(dataTable);
                            tempProduct.Id = (int)dataTable.Rows[0].ItemArray[0];
                            tempProduct.Name = dataTable.Rows[0].ItemArray[1].ToString();
                            tempProduct.Number = (int)dataTable.Rows[0].ItemArray[2];
                            if (dataTable.Rows[0].ItemArray[3].ToString() != "")
                                pokeMessage("Bu ürün zaten rafta!", MessageDialogIcon.Error, MessageDialogButtons.OK);
                            // ================================
                            dataTable.Clear();
                            dataAdapter = new SqlDataAdapter("Select * from Shelf where Status =0", sqlConnection);
                            dataAdapter.Fill(dataTable);
                            foreach (DataRow row in dataTable.Rows)
                            {
                                if (!(shelves.Contains((int)row.ItemArray[2])))
                                {
                                    Shelf temp = new Shelf();
                                    temp.Id = (int)row.ItemArray[0];
                                    temp.Number = (int)row.ItemArray[2];
                                    temp.Status = (bool)row.ItemArray[4];
                                    temp.SX = Convert.ToInt32(row.ItemArray[5]);
                                    temp.SY = Convert.ToInt32(row.ItemArray[6]);
                                    temp.SZ = Convert.ToInt32(row.ItemArray[7]);
                                    tempList.Add(temp);
                                }
                            }
                            Random random = new Random();
                            //num = random.Next(0, tempList.Count + 1);
                        }
                        else
                            pokeMessage("Harf yanlış girdiniz!", MessageDialogIcon.Error, MessageDialogButtons.OK);
                    }

                    catch (Exception ex)
                    {
                        pokeMessage(ex.Message, MessageDialogIcon.Error, MessageDialogButtons.OK);
                    }
                    finally
                    {
                        Task.Run(() =>
                        {
                            dropShelf.Id = 12;
                            dropShelf.Number = 11;
                            dropShelf.Status = false;
                            dropShelf.SX = 329;
                            dropShelf.SY = 0;
                            dropShelf.SZ = 807;
                            if (GetProduct(tempProduct, tempShelf, true))
                            {
                                Thread.Sleep(100);
                                DropProduct(tempProduct, dropShelf, false);
                            }
                        });
                    }
                }
            }
            else
                pokeMessage("Bağlantınızı kontrol ediniz!", MessageDialogIcon.Error, MessageDialogButtons.OK);
        }
        // =======================================
        private void btnDropProduct_Click(object sender, EventArgs e)
        {
            if (myPlc.IsConnected)
            {
                if (!bProcessExist)
                {
                    Product tempProduct = new Product();
                    Shelf tempShelf = new Shelf();
                    try
                    {
                        string sProductName = Interaction.InputBox("Almasını istediğiniz harfi giriniz!", "ASRS Uygulaması");
                        if (sProductName != "")
                        {
                            // ================================
                            if (sqlConnection.State != ConnectionState.Open)
                                sqlConnection.Open();
                            dataAdapter = new SqlDataAdapter("Select * from Product where Name ='" + sProductName + "'", sqlConnection);
                            dataTable = new DataTable();
                            dataAdapter.Fill(dataTable);
                            tempProduct.Id = (int)dataTable.Rows[0].ItemArray[0];
                            tempProduct.Name = dataTable.Rows[0].ItemArray[1].ToString();
                            tempProduct.Number = (int)dataTable.Rows[0].ItemArray[2];
                            tempProduct.ShelfId = (int)dataTable.Rows[0].ItemArray[3];
                            // ================================
                            dataTable.Clear();
                            dataAdapter = new SqlDataAdapter("Select * from Shelf where Id=" + tempProduct.ShelfId, sqlConnection);
                            dataAdapter.Fill(dataTable);
                            tempShelf.Id = (int)dataTable.Rows[0].ItemArray[0];
                            tempShelf.Number = (int)dataTable.Rows[0].ItemArray[2];
                            tempShelf.Status = (bool)dataTable.Rows[0].ItemArray[4];
                            tempShelf.SX = Convert.ToInt32(dataTable.Rows[0].ItemArray[5]);
                            tempShelf.SY = Convert.ToInt32(dataTable.Rows[0].ItemArray[6]);
                            tempShelf.SZ = Convert.ToInt32(dataTable.Rows[0].ItemArray[7]);
                        }
                        else
                            pokeMessage("Harf yanlış girdiniz!", MessageDialogIcon.Error, MessageDialogButtons.OK);
                    }

                    catch (Exception ex)
                    {
                        pokeMessage(ex.Message, MessageDialogIcon.Error, MessageDialogButtons.OK);
                    }
                    finally
                    {
                        Task.Run(() =>
                        {
                            if (GetProduct(tempProduct, tempShelf, false))
                            {
                                Thread.Sleep(100);
                                DropProduct(tempProduct, tempShelf, true);
                            }
                        });
                    }
                }
            }
            else
                pokeMessage("Bağlantınızı kontrol ediniz!", MessageDialogIcon.Error, MessageDialogButtons.OK);
        }
        // =======================================
        private void btnGetProduct_Click(object sender, EventArgs e)
        {
            if (myPlc.IsConnected)
            {
                if (!bProcessExist)
                {
                    Product tempProduct = new Product();
                    Shelf tempShelf = new Shelf();
                    int num = 0;
                    List<Shelf> tempList = new List<Shelf>();
                    try
                    {
                        string sProductName = Interaction.InputBox("Koyduğunuz harfi giriniz!", "ASRS Uygulaması");
                        if (sProductName != "")
                        {
                            // ================================
                            if (sqlConnection.State != ConnectionState.Open)
                                sqlConnection.Open();
                            dataAdapter = new SqlDataAdapter("Select * from Product where Name ='" + sProductName + "'", sqlConnection);
                            dataTable = new DataTable();
                            dataAdapter.Fill(dataTable);
                            tempProduct.Id = (int)dataTable.Rows[0].ItemArray[0];
                            tempProduct.Name = dataTable.Rows[0].ItemArray[1].ToString();
                            tempProduct.Number = (int)dataTable.Rows[0].ItemArray[2];
                            if (dataTable.Rows[0].ItemArray[3].ToString() != "")
                                pokeMessage("Bu ürün zaten rafta!", MessageDialogIcon.Error, MessageDialogButtons.OK);
                            // ================================
                            dataTable.Clear();
                            dataAdapter = new SqlDataAdapter("Select * from Shelf where Status =0", sqlConnection);
                            dataAdapter.Fill(dataTable);
                            foreach (DataRow row in dataTable.Rows)
                            {
                                if (!(shelves.Contains((int)row.ItemArray[2])))
                                {
                                    Shelf temp = new Shelf();
                                    temp.Id = (int)row.ItemArray[0];
                                    temp.Number = (int)row.ItemArray[2];
                                    temp.Status = (bool)row.ItemArray[4];
                                    temp.SX = Convert.ToInt32(row.ItemArray[5]);
                                    temp.SY = Convert.ToInt32(row.ItemArray[6]);
                                    temp.SZ = Convert.ToInt32(row.ItemArray[7]);
                                    tempList.Add(temp);
                                }
                            }
                            Random random = new Random();
                            num = random.Next(0, tempList.Count + 1);
                        }
                        else
                            pokeMessage("Harf yanlış girdiniz!", MessageDialogIcon.Error, MessageDialogButtons.OK);
                    }

                    catch (Exception ex)
                    {
                        pokeMessage(ex.Message, MessageDialogIcon.Error, MessageDialogButtons.OK);
                    }
                    finally
                    {
                        Task.Run(() =>
                        {
                            if (GetProduct(tempProduct, tempShelf, true))
                            {
                                Thread.Sleep(100);
                                DropProduct(tempProduct, tempList[num], false);
                            }
                        });
                    }
                }
            }
            else
                pokeMessage("Bağlantınızı kontrol ediniz!", MessageDialogIcon.Error, MessageDialogButtons.OK);
        }
        // =======================================
        private void btnLocateVestel_Click(object sender, EventArgs e)
        {
            try
            {
                if (myPlc.IsConnected)
                {
                    List<Product> products = new List<Product>();
                    for (int i = 5; i >= 0; i--)
                    {
                        Product temp = new Product();
                        if (sqlConnection.State != ConnectionState.Open)
                            sqlConnection.Open();
                        dataAdapter = new SqlDataAdapter("Select * from Product where Number =" + i, sqlConnection);
                        dataTable = new DataTable();
                        dataAdapter.Fill(dataTable);
                        temp.Id = (int)dataTable.Rows[0].ItemArray[0];
                        temp.Name = dataTable.Rows[0].ItemArray[1].ToString();
                        temp.Number = (int)dataTable.Rows[0].ItemArray[2];
                        temp.ShelfId = (int)dataTable.Rows[0].ItemArray[3];
                        products.Add(temp);
                    }
                    Task.Run(() =>
                    {
                        for (int i = 0; i < products.Count; i++)
                        {
                            Shelf getShelf = new Shelf();
                            Shelf dropShelf = new Shelf();
                            // =====================
                            // find get shelf
                            if (sqlConnection.State != ConnectionState.Open)
                                sqlConnection.Open();
                            dataAdapter = new SqlDataAdapter("Select * from Shelf where Id =" + products[i].ShelfId, sqlConnection);
                            dataTable = new DataTable();
                            dataAdapter.Fill(dataTable);
                            getShelf.Id = (int)dataTable.Rows[0].ItemArray[0];
                            getShelf.Number = (int)dataTable.Rows[0].ItemArray[1];
                            getShelf.Status = (bool)dataTable.Rows[0].ItemArray[2];
                            getShelf.SX = Convert.ToInt32(dataTable.Rows[0].ItemArray[3]);
                            getShelf.SY = Convert.ToInt32(dataTable.Rows[0].ItemArray[4]);
                            getShelf.SZ = Convert.ToInt32(dataTable.Rows[0].ItemArray[5]);
                            // =====================
                            // find drop shelf
                            if (sqlConnection.State != ConnectionState.Open)
                                sqlConnection.Open();
                            dataAdapter = new SqlDataAdapter("Select * from Shelf where Number =" + shelves[i], sqlConnection);
                            dataTable = new DataTable();
                            dataAdapter.Fill(dataTable);
                            dropShelf.Id = (int)dataTable.Rows[0].ItemArray[0];
                            dropShelf.Number = (int)dataTable.Rows[0].ItemArray[1];
                            dropShelf.Status = (bool)dataTable.Rows[0].ItemArray[2];
                            dropShelf.SX = Convert.ToInt32(dataTable.Rows[0].ItemArray[3]);
                            dropShelf.SY = Convert.ToInt32(dataTable.Rows[0].ItemArray[4]);
                            dropShelf.SZ = Convert.ToInt32(dataTable.Rows[0].ItemArray[5]);
                            if ((dropShelf.SX != getShelf.SX && dropShelf.SZ != getShelf.SZ) ||
                               (dropShelf.SX == getShelf.SX && dropShelf.SZ != getShelf.SZ) ||
                               (dropShelf.SX != getShelf.SX && dropShelf.SZ == getShelf.SZ))
                            {
                                if (GetProduct(products[i], getShelf, false))
                                {
                                    Thread.Sleep(100);
                                    DropProduct(products[i], dropShelf, false);
                                }
                            }
                        }
                    });

                }
                else
                    pokeMessage("Bağlantınızı kontrol ediniz!", MessageDialogIcon.Error, MessageDialogButtons.OK);
            }
            catch (Exception ex)
            {
                pokeMessage(ex.Message, MessageDialogIcon.Error, MessageDialogButtons.OK);
            }

        }
        // =======================================
        private void btnShuffle_Click(object sender, EventArgs e)
        {
            if (myPlc.IsConnected)
            {
                if (!bProcessExist)
                {
                    Product tempProduct = new Product();
                    Shelf tempShelf = new Shelf();
                    int num = 0;
                    List<Shelf> tempList = new List<Shelf>();
                    List<Product> products = new List<Product>();
                    try
                    {
                        for (int i = 5; i >= 0; i--)
                        {
                            Product temp = new Product();
                            if (sqlConnection.State != ConnectionState.Open)
                                sqlConnection.Open();
                            dataAdapter = new SqlDataAdapter("Select * from Product where Number =" + i, sqlConnection);
                            dataTable = new DataTable();
                            dataAdapter.Fill(dataTable);
                            temp.Id = (int)dataTable.Rows[0].ItemArray[0];
                            temp.Name = dataTable.Rows[0].ItemArray[1].ToString();
                            temp.Number = (int)dataTable.Rows[0].ItemArray[2];
                            temp.ShelfId = (int)dataTable.Rows[0].ItemArray[3];
                            products.Add(temp);
                        }
                        // ================================
                        dataTable.Clear();
                        dataAdapter = new SqlDataAdapter("Select * from Shelf where Status =0", sqlConnection);
                        dataAdapter.Fill(dataTable);
                        foreach (DataRow row in dataTable.Rows)
                        {
                            if (!(shelves.Contains((int)row.ItemArray[2])))
                            {
                                Shelf temp = new Shelf();
                                temp.Id = (int)row.ItemArray[0];
                                temp.Number = (int)row.ItemArray[2];
                                temp.Status = (bool)row.ItemArray[4];
                                temp.SX = Convert.ToInt32(row.ItemArray[5]);
                                temp.SY = Convert.ToInt32(row.ItemArray[6]);
                                temp.SZ = Convert.ToInt32(row.ItemArray[7]);
                                tempList.Add(temp);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        pokeMessage(ex.Message, MessageDialogIcon.Error, MessageDialogButtons.OK);
                    }
                    finally
                    {
                        Task.Run(() =>
                        {
                            for (int i = 0; i < products.Count; i++)
                            {
                                Random random = new Random();
                                num = random.Next(0, tempList.Count + 1);
                                Shelf getShelf = new Shelf();
                                Shelf dropShelf = new Shelf();
                                // =====================
                                // find get shelf
                                if (sqlConnection.State != ConnectionState.Open)
                                    sqlConnection.Open();
                                dataAdapter = new SqlDataAdapter("Select * from Shelf where Id =" + products[i].ShelfId, sqlConnection);
                                dataTable = new DataTable();
                                dataAdapter.Fill(dataTable);
                                getShelf.Id = (int)dataTable.Rows[0].ItemArray[0];
                                getShelf.Number = (int)dataTable.Rows[0].ItemArray[1];
                                getShelf.Status = (bool)dataTable.Rows[0].ItemArray[2];
                                getShelf.SX = Convert.ToInt32(dataTable.Rows[0].ItemArray[3]);
                                getShelf.SY = Convert.ToInt32(dataTable.Rows[0].ItemArray[4]);
                                getShelf.SZ = Convert.ToInt32(dataTable.Rows[0].ItemArray[5]);
                                // =====================
                                // find drop shelf
                                if (sqlConnection.State != ConnectionState.Open)
                                    sqlConnection.Open();
                                dataAdapter = new SqlDataAdapter("Select * from Shelf where Id =" + tempList[num].Id, sqlConnection);
                                dataTable = new DataTable();
                                dataAdapter.Fill(dataTable);
                                dropShelf.Id = (int)dataTable.Rows[0].ItemArray[0];
                                dropShelf.Number = (int)dataTable.Rows[0].ItemArray[1];
                                dropShelf.Status = (bool)dataTable.Rows[0].ItemArray[2];
                                dropShelf.SX = Convert.ToInt32(dataTable.Rows[0].ItemArray[3]);
                                dropShelf.SY = Convert.ToInt32(dataTable.Rows[0].ItemArray[4]);
                                dropShelf.SZ = Convert.ToInt32(dataTable.Rows[0].ItemArray[5]);
                                if (shelves.Contains(getShelf.Number))
                                {
                                    if (GetProduct(products[i], getShelf, false))
                                    {
                                        Thread.Sleep(100);
                                        DropProduct(products[i], dropShelf, false);
                                    }
                                }
                                tempList.Remove(tempList[num]);
                            }
                        });
                    }
                }
            }
            else
                pokeMessage("Bağlantınızı kontrol ediniz!", MessageDialogIcon.Error, MessageDialogButtons.OK);
        }
        // =======================================
        #endregion
        // =======================================
        #region ' Sub-Methods '
        // =======================================
        private DialogResult pokeMessage(string sMessage, MessageDialogIcon messageBoxIcon, MessageDialogButtons messageBoxButtons)
        {
            Guna2MessageDialog MessageBoxG = new Guna2MessageDialog();
            MessageBoxG.Icon = messageBoxIcon;
            MessageBoxG.Buttons = messageBoxButtons;
            MessageBoxG.Style = MessageDialogStyle.Default;
            return MessageBoxG.Show(sMessage, "ASRS Uygulaması");
        }
        // =======================================
        private void GetData()
        {
            while (true)
            {
                try
                {
                    myPlc.ReadClass(plcToPc, 9);
                    // ==============
                    // x coordinate
                    pbXpoint.X = Convert.ToInt32(Map(plcToPc.ActX, 68, 850, 325, 1640));
                    pbXpoint.X = Convert.ToInt32(Map(pbXpoint.X, 0, 1640, 1640, 0));
                    pbXpoint.X = pbXpoint.X + 150;
                    pbXpoint.Y = pbX.Location.Y;
                    pbX.Location = pbXpoint;
                    // ==============
                    // z coordinate
                    pbZpoint.X = pbX.Location.X + 73;
                    pbZpoint.Y = Convert.ToInt32(Map(plcToPc.ActZ, 29, 807, 144, 597));
                    pbZpoint.Y = Convert.ToInt32(Map(pbZpoint.Y, 144, 597, 597, 144));
                    pbZpoint.Y = pbZpoint.Y - 5;
                    pbZ.Location = pbZpoint;
                }
                catch (Exception ex)
                {
                    pokeMessage(ex.Message, MessageDialogIcon.Error, MessageDialogButtons.OK);
                    break;
                }
                Thread.Sleep(20);
            }
        }
        // =======================================
        public decimal Map(decimal value, decimal fromSource, decimal toSource, decimal fromTarget, decimal toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }
        // =======================================
        private bool GetProduct(Product product, Shelf shelf, bool IsConveyor)
        {
            while (true)
            {
                switch (getProductProcess)
                {
                    case GetProductRequestProcess.Initialize:
                        {
                            bProcessExist = true;
                            getProductProcess = GetProductRequestProcess.CheckCraneIsNotBusy;
                            break;
                        }
                    case GetProductRequestProcess.CheckCraneIsNotBusy:
                        {
                            getProductProcess = GetProductRequestProcess.SendRequest;
                            Thread.Sleep(500);
                            break;
                        }
                    case GetProductRequestProcess.SendRequest:
                        {
                            if (!plcToPc.bTaskExist)
                            {
                                pcToPlc.bTaskRequest = true;
                                pcToPlc.bGetProduct = true;
                                if (IsConveyor)
                                {
                                    pcToPlc.X = 0;
                                    pcToPlc.Y = 0;
                                    pcToPlc.Z = 0;
                                }
                                else
                                {
                                    pcToPlc.X = Convert.ToInt32(shelf.SX);
                                    pcToPlc.Y = Convert.ToInt32(shelf.SY);
                                    pcToPlc.Z = Convert.ToInt32(shelf.SZ);
                                }
                                myPlc.WriteClass(pcToPlc, 8);
                            }
                            if (plcToPc.bTaskTaken)
                            {
                                getProductProcess = GetProductRequestProcess.WaitForRequestDone;
                                pcToPlc.bTaskRequest = false;
                                pcToPlc.bGetProduct = false;
                                myPlc.WriteClass(pcToPlc, 8);
                            }
                            break;
                        }
                    case GetProductRequestProcess.WaitForRequestDone:
                        {
                            if (plcToPc.bTaskDone)
                            {
                                pcToPlc.X = 0;
                                pcToPlc.Y = 0;
                                pcToPlc.Z = 0;
                                if (!IsConveyor)
                                {
                                    if (sqlConnection.State != ConnectionState.Open)
                                        sqlConnection.Open();
                                    SqlCommand sqlCommand = new SqlCommand("Update Shelf SET Status=0 where Id =" + product.ShelfId, sqlConnection);
                                    sqlCommand.ExecuteNonQuery();
                                    SqlCommand sqlCommand2 = new SqlCommand("Update Product SET ShelfId=NULL where Id =" + product.Id, sqlConnection);
                                    sqlCommand2.ExecuteNonQuery();
                                }
                                myPlc.WriteClass(pcToPlc, 8);
                                getProductProcess = GetProductRequestProcess.Done;
                            }
                            break;
                        }
                    case GetProductRequestProcess.Done:
                        {
                            if (product != null)
                                asrsProduct = product;
                            break;
                        }
                }
                if (getProductProcess == GetProductRequestProcess.Done)
                {
                    getProductProcess = GetProductRequestProcess.Initialize;
                    bProcessExist = false;
                    return true;
                }
                Thread.Sleep(500);
            }
        }
        // =======================================
        private bool DropProduct(Product product, Shelf shelf, bool IsConveyor)
        {
            while (true)
            {
                switch (dropProductProcess)
                {
                    case DropProductRequestProcess.Initialize:
                        {
                            Thread.Sleep(100);
                            bProcessExist = true;
                            dropProductProcess = DropProductRequestProcess.CheckCraneIsNotBusy;
                            break;
                        }
                    case DropProductRequestProcess.CheckCraneIsNotBusy:
                        {
                            if (!plcToPc.bTaskExist)
                                dropProductProcess = DropProductRequestProcess.SendRequest;
                            else
                                Thread.Sleep(500);
                            break;
                        }
                    case DropProductRequestProcess.SendRequest:
                        {
                            if (!plcToPc.bTaskExist)
                            {
                                pcToPlc.bTaskRequest = true;
                                pcToPlc.bDropProduct = true;
                                if (IsConveyor)
                                {
                                    pcToPlc.X = 0;
                                    pcToPlc.Y = 0;
                                    pcToPlc.Z = 0;
                                }
                                else
                                {
                                    pcToPlc.X = Convert.ToInt32(shelf.SX);
                                    pcToPlc.Y = Convert.ToInt32(shelf.SY);
                                    pcToPlc.Z = Convert.ToInt32(shelf.SZ);
                                }
                                myPlc.WriteClass(pcToPlc, 8);
                            }
                            if (plcToPc.bTaskTaken)
                            {
                                dropProductProcess = DropProductRequestProcess.WaitForRequestDone;
                                pcToPlc.bTaskRequest = false;
                                pcToPlc.bDropProduct = false;
                                myPlc.WriteClass(pcToPlc, 8);
                            }
                            break;
                        }
                    case DropProductRequestProcess.WaitForRequestDone:
                        {
                            if (plcToPc.bTaskDone)
                            {
                                pcToPlc.bTaskRequest = false;
                                pcToPlc.bDropProduct = false;
                                pcToPlc.X = 0;
                                pcToPlc.Y = 0;
                                pcToPlc.Z = 0;
                                myPlc.WriteClass(pcToPlc, 8);
                                if (IsConveyor)
                                {
                                    if (sqlConnection.State != ConnectionState.Open)
                                        sqlConnection.Open();
                                    SqlCommand sqlCommand = new SqlCommand("Update Shelf SET Status=0 where Id =" + product.ShelfId, sqlConnection);
                                    sqlCommand.ExecuteNonQuery();
                                    SqlCommand sqlCommand2 = new SqlCommand("Update Product SET ShelfId=NULL where Id =" + product.Id, sqlConnection);
                                    sqlCommand2.ExecuteNonQuery();
                                }
                                else
                                {
                                    if (sqlConnection.State != ConnectionState.Open)
                                        sqlConnection.Open();
                                    SqlCommand sqlCommand = new SqlCommand("Update Shelf SET Status=1 where Id =" + shelf.Id, sqlConnection);
                                    sqlCommand.ExecuteNonQuery();
                                    SqlCommand sqlCommand2 = new SqlCommand("Update Product SET ShelfId= " + shelf.Id + " where Id = " + product.Id, sqlConnection);
                                    sqlCommand2.ExecuteNonQuery();
                                }

                                dropProductProcess = DropProductRequestProcess.Done;
                            }
                            break;
                        }
                    case DropProductRequestProcess.Done:
                        {
                            break;
                        }
                }
                if (dropProductProcess == DropProductRequestProcess.Done)
                {
                    bProcessExist = false;
                    dropProductProcess = DropProductRequestProcess.Initialize;
                    return true;
                }
                Thread.Sleep(500);
            }
        }
        #endregion
        // =======================================
        #region ' Form Methods '
        private void Form1_Load(object sender, EventArgs e)
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            lblProcess.Text = "";
            CheckForIllegalCrossThreadCalls = false;
            myPlc = new Plc(CpuType.S71500, "192.168.127.100", 0, 1);
            sqlConnection = new SqlConnection("Data Source=192.168.127.40,49170; Initial Catalog=ASRSConsol;Persist Security Info=False; User ID=AGVTEAM; Password=agv.1234;");
            thread = new Thread(GetData);
            shelfTimer.Start();
        }
        // =======================================
        [Obsolete]
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            pcToPlc.bPcActive = false;
            if (myPlc.IsConnected)
                myPlc.WriteClass(pcToPlc, 8);
            if (thread.ThreadState == ThreadState.WaitSleepJoin)
                thread.Abort();
            else if (thread.ThreadState == ThreadState.Suspended)
            {
                thread.Resume();
                thread.Abort();
            }
        }
        // =======================================
        private void shelfTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (plcToPc.bAutomaticMode)
                    lblMode.Text = "Otomatik";
                else
                    lblMode.Text = "Manual";
                if (plcToPc.bTaskDeleted)
                    lblTaskStatus.Text = "Görev silindi!";
                else if (plcToPc.bTaskDone)
                    lblTaskStatus.Text = "Görev tamamlandı!";
                else if (plcToPc.bTaskTaken)
                    lblTaskStatus.Text = "Görev alındı!";
                else if (plcToPc.bTaskExist)
                    lblTaskStatus.Text = "Görev var!";
                else
                    lblTaskStatus.Text = "Görev bekleniyor!";
                // ==============
                // shelf status update
                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();
                dataAdapter = new SqlDataAdapter("Select * From Shelf", sqlConnection);
                dataTable = new DataTable();
                dataAdapter.Fill(dataTable);
                sqlConnection.Close();
            }
            catch (Exception ex)
            {
                pokeMessage(ex.Message, MessageDialogIcon.Error, MessageDialogButtons.OK);
            }
            finally
            {
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    string panel = "p" + dataRow.ItemArray[1];
                    var panel1 = this.Controls.Find(panel, true)[0];

                    if ((bool)dataRow.ItemArray[2])
                    {
                        if (sqlConnection.State != ConnectionState.Open)
                            sqlConnection.Open();
                        dataAdapter = new SqlDataAdapter("Select Name From Product where ShelfId=" + dataRow.ItemArray[0], sqlConnection);
                        dataTable = new DataTable();
                        dataAdapter.Fill(dataTable);
                        Label lblLetter = new Label();
                        lblLetter.AutoSize = true;
                        lblLetter.TextAlign = ContentAlignment.MiddleRight;
                        lblLetter.ForeColor = Color.DarkRed;
                        lblLetter.Font = new Font("Corbel", 100, FontStyle.Bold);
                        if (dataTable.Rows[0].ItemArray[0].ToString() == "EE")
                            lblLetter.Text = "E";
                        else
                            lblLetter.Text = dataTable.Rows[0].ItemArray[0].ToString();
                        if (panel1.Controls.Count > 0)
                        {

                        }
                        else
                        {
                            panel1.Controls.Add(lblLetter);
                        }

                        panel1.BackColor = Color.LightCoral;
                    }
                    else
                    {
                        if (panel1.Controls.Count > 0)
                        {
                            panel1.Controls.Remove(panel1.Controls[0]);
                        }
                        panel1.BackColor = Color.LightGreen;
                    }
                }

            }
        }
        #endregion
        // =======================================
    }
}
