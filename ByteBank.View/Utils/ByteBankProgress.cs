using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ByteBank.View.Utils
{
    public class ByteBankProgress<T> : IProgress<T>
    {

        private readonly Action<T> _handler;
        private readonly TaskScheduler _taskScheduler;

        public ByteBankProgress(Action<T> handler)
        {
                _taskScheduler = TaskScheduler.FromCurrentSynchronizationContext(); //recupera a thread da interface para poder executar o "progresso" da barra na interface
                _handler = handler;
        }


        public void Report(T value) //essa é a parte que irá dar a "resposta" da classe
        {
            Task.Factory.StartNew(() => 
             _handler(value), 
            System.Threading.CancellationToken.None,
            TaskCreationOptions.None, 
            _taskScheduler);


        }
    }
}
