using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using ByteBank.View.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ByteBank.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        private async void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
           
            BtnCancelar.IsEnabled = true;
            BtnProcessar.IsEnabled = false;

            _cts = new CancellationTokenSource();

            var contas = r_Repositorio.GetContaClientes();

            PgsProgresso.Maximum = contas.Count(); //definir tamanho máximo da barra de progresso

            LimparView();
            //AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;

            //as duas linhas abaixo fazem o mesmo, a diferença é que em uma usamos o componente pronto e na outra criamos o componente
            var progresso = new Progress<string>(str => PgsProgresso.Value++);
            //var byteBankProgress = new ByteBankProgress<string>(str => PgsProgresso.Value++);

            //var resultado = await ConsolidarContas_2(contas, byteBankProgress);
            var resultado = await ConsolidarContas_2(contas, progresso, _cts.Token);
            var fim = DateTime.Now;
            AtualizarView_2(resultado, fim - inicio);
            BtnProcessar.IsEnabled = true;

        }
        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            BtnCancelar.IsEnabled = false;
            _cts.Cancel();
        }

        private void AtualizarView(List<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{elapsedTime.Seconds}.{elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }

        private void AtualizarView_2(String[] result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{elapsedTime.Seconds}.{elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count()} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }

        private Task<List<string>> ConsolidarContas(IEnumerable<ContaCliente> listaContas)
        {
            var resultado = new List<string>();

            var tasks = listaContas.Select(ct =>
            {
                return Task.Factory.StartNew(() =>
                {
                    var resultadoConta = r_Servico.ConsolidarMovimentacao(ct);
                    resultado.Add(resultadoConta);
                });
            });

            return Task.WhenAll(tasks).ContinueWith(task =>
            {
                return resultado;
            });

        }


        private async Task<string[]> ConsolidarContas_2(IEnumerable<ContaCliente> listaContas)
        {
            var resultado = new List<string>();
            var taskSchedulerGui = TaskScheduler.FromCurrentSynchronizationContext(); //vamo pegar a thread da interface para poder atualizar a barra de progresso

            //var tasks = listaContas.Select(ct => Task.Factory.StartNew(() =>
            //         r_Servico.ConsolidarMovimentacao(ct)
            //    )
            //);

            var tasks = listaContas.Select(ct => Task.Factory.StartNew(() =>
            {
                var resultadoConsolidado = r_Servico.ConsolidarMovimentacao(ct);


                Task.Factory.StartNew(() => PgsProgresso.Value++,
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    taskSchedulerGui);

                return resultadoConsolidado;
            })
            );

            return await Task.WhenAll(tasks);
        }


        private async Task<string[]> ConsolidarContas_2(IEnumerable<ContaCliente> listaContas, IProgress<string> reportProgresso, CancellationToken cts)
        {
            var resultado = new List<string>();
           
            var tasks = listaContas.Select(conta => Task.Factory.StartNew(() =>
            {
                if (cts.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cts);
                }

                var resultadoConsolidado = r_Servico.ConsolidarMovimentacao(conta, cts);

                reportProgresso.Report(resultadoConsolidado);

                if (cts.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cts);
                }

                return resultadoConsolidado;
            })
            );

            return await Task.WhenAll(tasks);
        }


        private void LimparView()
        {
            LstResultados.ItemsSource = null;
            TxtTempo.Text = null;
            PgsProgresso.Value = 0;
        }


        private void BtnProcessar_1()
        {


            BtnCancelar.IsEnabled = true;
            BtnProcessar.IsEnabled = false;

            _cts = new CancellationTokenSource();

            var contas = r_Repositorio.GetContaClientes();

            PgsProgresso.Maximum = contas.Count(); //definir tamanho máximo da barra de progresso

            LimparView();
            //AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;


            var result = new List<string>();

            var qtdItensThreads = contas.Count() / 4;

            var contas_parte1 = contas.Take(qtdItensThreads);                                   // pega os primeiros X elementos
            var contas_parte2 = contas.Skip(qtdItensThreads).Take(qtdItensThreads);             // pula os primeiros X elementos e pega o restante
            var contas_parte3 = contas.Skip(qtdItensThreads * 2).Take(qtdItensThreads);
            var contas_parte4 = contas.Skip(qtdItensThreads * 3);

            Thread thread_parte1 = new Thread(() =>
            {
                foreach (var conta in contas_parte1)
                {
                    var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                    result.Add(resultadoProcessamento);
                }
            });

            Thread thread_parte2 = new Thread(() =>
            {
                foreach (var conta in contas_parte2)
                {
                    var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                    result.Add(resultadoProcessamento);
                }
            });

            Thread thread_parte3 = new Thread(() =>
            {
                foreach (var conta in contas_parte3)
                {
                    var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                    result.Add(resultadoProcessamento);
                }
            });

            Thread thread_parte4 = new Thread(() =>
            {
                foreach (var conta in contas_parte4)
                {
                    var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                    result.Add(resultadoProcessamento);
                }
            });

            thread_parte1.Start();
            thread_parte2.Start();

            while (thread_parte1.IsAlive || thread_parte2.IsAlive || thread_parte3.IsAlive || thread_parte4.IsAlive)
            {
                Thread.Sleep(250);
            }
        }


        private void BtnProcessar_2()
        {
            BtnCancelar.IsEnabled = true;
            BtnProcessar.IsEnabled = false;

            _cts = new CancellationTokenSource();

            var contas = r_Repositorio.GetContaClientes();

            PgsProgresso.Maximum = contas.Count(); //definir tamanho máximo da barra de progresso

            LimparView();
            //AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;


            var result = new List<string>();



            //com essa função é possível identifcar qual thread está executando a interface gráfica. Se a linha fosse executada em outro ponto, como na chamada da variável contasTarefas, o resultado seria diferente
            var taskSchedulerUI = TaskScheduler.FromCurrentSynchronizationContext();


           
            var contasTarefas = contas.Select(conta =>
            {
                return Task.Factory.StartNew(() =>
                {
                    var resultadoConta = r_Servico.ConsolidarMovimentacao(conta);
                    result.Add(resultadoConta);
                });
            }).ToArray();       //toarray() obriga o Linq a executar a tarefa

            //Task.WaitAll(contasTarefas);      //faz com que aguarde todas as tarefas. Não tem retorno. Para a execução da thread principal até que todas as tasks finalizem
            Task.WhenAll(contasTarefas)        //faz um tratamento semelhante ao waitall, porém retorna uma tarefa que espera o término das tarefas passadas por parâmetro
                .ContinueWith(task =>
                {        //só vai continuar quando todas as tarefas terminarem, ou seja, quando a tarefa filho terminar. Dentro desse delegate é possível capturar informações das execuções, como exception

                    //var fim = DateTime.Now;
                    //AtualizarView(result, fim - inicio);

                }, taskSchedulerUI)     //aqui indico que o item vai acontecer na thread da interface gráfica     
                .ContinueWith(task =>
                {
                    BtnProcessar.IsEnabled = true;
                }, taskSchedulerUI);


            ConsolidarContas(contas).ContinueWith(task =>
            {
                //var fim = DateTime.Now;
                // var resultado = task.Result;
                // AtualizarView(resultado, fim - inicio);

            });

        }
    }
}
