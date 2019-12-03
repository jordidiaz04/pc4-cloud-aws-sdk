using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace SQS_Ventas
{
    public partial class Form1 : Form
    {
        private AmazonSQSClient sqsClient;
        private CreateQueueRequest sqsRequest;
        private ListQueuesRequest listQueues;
        private SendMessageRequest messageRequest;
        private String url;
        private String queue = "grupo1-pc4.fifo";


        public Form1()
        {
            InitializeComponent();
        }

        private bool checkQueueExist()
        {
            try
            {
                bool exist = false;
                listQueues = new ListQueuesRequest();
                var lista = sqsClient.ListQueues(listQueues);

                List<String> colas = new List<String>();
                foreach (var urls in lista.QueueUrls)
                {
                    String[] array = urls.Split('/');                    
                    String nombre = array[array.Length - 1];
                    if(nombre == queue)
                    {
                        url = urls.Replace(array[array.Length - 1], "");
                        exist = true;
                    }
                }

                return exist;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                return false;
            }
        }

        private bool checkFields()
        {
            if (txtCliente.Text.Trim() == String.Empty || txtProducto.Text.Trim() == String.Empty || txtCantidad.Text.Trim() == String.Empty ||
                txtPrecio.Text.Trim() == String.Empty || txtMonto.Text.Trim() == String.Empty)
            {
                return false;
            }
            return true;
        }

        private void clearFields()
        {
            foreach (Control item in Controls)
            {
                if(item is TextBox)
                {
                    item.Text = String.Empty;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                sqsClient = new AmazonSQSClient("AKIAQKS6V3RVOBQR3XJC", "/rqjkje3pZ5vyXNfyuZXxNQyFlkYO8ywQamljYCX");

                if (!checkQueueExist())
                {
                    Dictionary<String, String> attributes = new Dictionary<string, string>();
                    attributes.Add("FifoQueue", "true");
                    attributes.Add("ContentBasedDeduplication", "true");
                    sqsRequest = new CreateQueueRequest()
                    {
                        QueueName = queue,
                        Attributes = attributes

                    };
                    var result = sqsClient.CreateQueue(sqsRequest);
                    url = result.QueueUrl;
                }
                else
                {
                    url = url + queue;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            try
            {
                if (checkFields())
                {
                    VentaBE ventaBE = new VentaBE()
                    {
                        Cliente = txtCliente.Text.Trim(),
                        Producto = txtProducto.Text.Trim(),
                        Cantidad = Convert.ToInt32(txtCantidad.Text),
                        Precio = Convert.ToDecimal(txtPrecio.Text),
                        Monto = Convert.ToDecimal(txtMonto.Text)
                    };
                    String ventaJSON = new JavaScriptSerializer().Serialize(ventaBE);

                    messageRequest = new SendMessageRequest()
                    {
                        QueueUrl = url,
                        MessageBody = ventaJSON,
                        MessageGroupId = "grupo1-pc4"
                    };

                    sqsClient.SendMessage(messageRequest);
                    MessageBox.Show("Venta enviada.");
                    clearFields();
                }
                else
                {
                    MessageBox.Show("Debe completar todos los campos.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void txtCantidad_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void txtPrecio_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }

            if((Keys)e.KeyChar == Keys.Enter || (Keys)e.KeyChar == Keys.Tab)
            {
                if (txtCantidad.Text.Trim() == String.Empty) txtCantidad.Text = "1.00";
                if (txtPrecio.Text.Trim() == String.Empty) txtPrecio.Text = "1.00";
                
                decimal monto = Convert.ToInt32(txtCantidad.Text.Trim()) * Convert.ToDecimal(txtPrecio.Text.Trim());
                txtPrecio.Text = Convert.ToDecimal(txtPrecio.Text.Trim()).ToString("#.00");
                txtMonto.Text = monto.ToString("#.00");
                txtMonto.Focus();
            }
        }
    }
}

public class VentaBE
{
    public String Cliente { get; set; }
    public String Producto { get; set; }
    public Int32 Cantidad { get; set; }
    public Decimal Precio { get; set; }
    public Decimal Monto { get; set; }
}