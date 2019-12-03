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
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;

namespace SQS_Delivery
{
    public partial class Form1 : Form
    {
        private AmazonSQSClient sqsClient;
        private CreateQueueRequest sqsRequest;
        private ListQueuesRequest listQueues;
        private ReceiveMessageRequest receiveMessageRequest;
        private AmazonDynamoDBClient dbClient;
        private String url;
        private String queue = "grupo1-pc4.fifo";
        private String tableName = "grupo1-pc4-ventas";
        private Int32 countList = 0;
        private List<VentaBE> lista;

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
                    if (nombre == queue)
                    {
                        url = urls.Replace(array[array.Length - 1], "");
                        exist = true;
                    }
                }

                return exist;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                return false;
            }
        }

        private bool checkDBExist()
        {
            try
            {
                Table ventaTable = Table.LoadTable(dbClient, tableName);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void listMessages()
        {
            receiveMessageRequest = new ReceiveMessageRequest()
            {
                QueueUrl = url,
                MaxNumberOfMessages = 10
            };

            var response = sqsClient.ReceiveMessage(receiveMessageRequest);

            foreach (var item in response.Messages)
            {
                VentaBE ventaBE = new JavaScriptSerializer().Deserialize<VentaBE>(item.Body);
                ventaBE.Id = item.MessageId;
                int index = lista.FindIndex(f => f.Id == item.MessageId);
                if (index < 0)
                {
                    lista.Add(ventaBE);
                }
            }

            if (countList != lista.Count)
            {
                BindingSource bs = new BindingSource();
                bs.DataSource = lista;
                dgvVentas.DataSource = bs;
                bs.ResetBindings(false);
                countList = lista.Count;
            }
        }

        private void createTable()
        {
            var response = dbClient.CreateTable(new CreateTableRequest
            {
                TableName = tableName,
                AttributeDefinitions = new List<AttributeDefinition>()
                              {
                                  new AttributeDefinition
                                  {
                                      AttributeName = "Id",
                                      AttributeType = "S"
                                  }
                              },
                KeySchema = new List<KeySchemaElement>()
                              {
                                  new KeySchemaElement
                                  {
                                      AttributeName = "Id",
                                      KeyType = "HASH"
                                  }
                              },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 10,
                    WriteCapacityUnits = 5
                }
            });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                sqsClient = new AmazonSQSClient("AKIAQKS6V3RVOBQR3XJC", "/rqjkje3pZ5vyXNfyuZXxNQyFlkYO8ywQamljYCX");
                dbClient = new AmazonDynamoDBClient("AKIAQKS6V3RVOBQR3XJC", "/rqjkje3pZ5vyXNfyuZXxNQyFlkYO8ywQamljYCX");
                lista = new List<VentaBE>();

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

                if (!checkDBExist())
                {
                    createTable();
                }

                listMessages();
                timer1.Enabled = true;
                timer1.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            listMessages();
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (var item in lista)
                {
                    Table ventaTable = Table.LoadTable(dbClient, tableName);
                    var venta = new Document();
                    venta["Id"] = item.Id;
                    venta["Cliente"] = item.Cliente;
                    venta["Producto"] = item.Producto;
                    venta["Cantidad"] = item.Cantidad;
                    venta["Precio"] = item.Precio;
                    venta["Monto"] = item.Monto;
                    ventaTable.PutItem(venta);
                }

                MessageBox.Show("Información enviada a la base de datos");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}

public class VentaBE
{
    public String Id { get; set; }
    public String Cliente { get; set; }
    public String Producto { get; set; }
    public Int32 Cantidad { get; set; }
    public Decimal Precio { get; set; }
    public Decimal Monto { get; set; }
}