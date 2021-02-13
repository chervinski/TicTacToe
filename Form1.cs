using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TicTacToe
{
	public partial class Form1 : Form
	{
		private static readonly char cross = '❌', nought = '◯';
		private static readonly int port = 8888;

		private char symbol;
		private bool turn, disconnect;
		private Socket server, handler;
		private bool Turn
		{
			get => turn;
			set
			{
				turn = value;
				turnLabel.Text = turn ? $"Your turn ({symbol})" : $"Waiting for turn ({(symbol == cross ? nought : cross)})";
			}
		}
		public Form1() => InitializeComponent();
		private void Reset()
		{
			symbol = '\0';
			foreach (Button button in panel1.Controls)
				button.Text = string.Empty;
			openButton.Enabled = connectButton.Enabled = true;
			closeButton.Enabled = disconnectButton.Enabled = false;
			localIPLabel.Text = turnLabel.Text = string.Empty;
		}
		private bool HandleWinner()
		{
			char[] cells = panel1.Controls.Cast<Button>().Select(x => x.Text == string.Empty ? '\0' : x.Text[0]).ToArray();
			char winner;

			for (int i = 0; i < 9; i += 3)
				if (cells[i] != '\0' && cells[i] == cells[i + 1] && cells[i] == cells[i + 2])
				{
					winner = cells[i];
					goto end;
				}

			for (int i = 0; i < 3; ++i)
				if (cells[i] != '\0' && cells[i] == cells[i + 3] && cells[i] == cells[i + 6])
				{
					winner = cells[i];
					goto end;
				}

			winner = cells[4] != '\0' && (
				cells[4] == cells[0] && cells[4] == cells[8] ||
				cells[4] == cells[2] && cells[4] == cells[6]) ? cells[4] : '\0';

			end:
			bool draw = winner == '\0' && cells.All(x => x != '\0');
			if (winner != '\0' || draw)
			{
				MessageBox.Show(draw ? "Draw!" : winner + " won!");
				foreach (Button b in panel1.Controls)
					b.Text = string.Empty;
				Turn = symbol == cross;
				return true;
			}
			return false;
		}
		private void Cell_Click(object sender, EventArgs e)
		{
			Button button = sender as Button;
			if (button.Text == string.Empty && symbol != '\0' && Turn)
			{
				handler.Send(new []{ (byte)panel1.Controls.IndexOf(button) });
				button.Text = symbol.ToString();

				if (!HandleWinner())
					Turn = false;
			}
		}
		private async void Connect_Click(object sender, EventArgs e)
		{
			try
			{
				using (handler = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
				{
					handler.Connect(new IPEndPoint(IPAddress.Parse(textBox1.Text), port));

					symbol = nought;
					Turn = false;
					disconnectButton.Enabled = true;
					connectButton.Enabled = openButton.Enabled = false;

					await ListenAsync();
				}
			}
			catch (Exception ex)
			{
				if (disconnect) disconnect = false;
				else MessageBox.Show(ex.Message);
				Reset();
			}
		}
		private async void Open_Click(object sender, EventArgs e)
		{
			try
			{
				localIPLabel.Text = "Your local IP: " + Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork).ToString();

				closeButton.Enabled = true;
				connectButton.Enabled = openButton.Enabled = false;

				using (server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
				{
					server.Bind(new IPEndPoint(IPAddress.Any, port));
					server.Listen(1);
					using (handler = await server.AcceptAsync())
					{
						symbol = cross;
						Turn = true;
						await ListenAsync();
					}
				}
			}
			catch (Exception ex)
			{
				if (disconnect) disconnect = false;
				else MessageBox.Show(ex.Message);
				Reset();
			}
		}
		private async Task ListenAsync()
		{
			while (true)
			{
				ArraySegment<byte> segment = new ArraySegment<byte>(new byte[1]);
				await handler.ReceiveAsync(segment, SocketFlags.None);
				byte message = segment.Array[0];

				panel1.Controls[message].Text = (symbol == cross ? nought : cross).ToString();
				if (!HandleWinner())
					Turn = true;
			}
		}
		private void Disconnect_Click(object sender, EventArgs e)
		{
			disconnect = true;
			server?.Close();
			handler?.Close();
		}
	}
}
