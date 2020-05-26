using System;
using System.Collections.Generic;
using System.Text;

namespace CadFoundation
{
    public class TextMessagePump
    {
        private HashSet<IObserver<string>> observers = new HashSet<IObserver<string>>();

        public void Register(IObserver<string> observer, String initMessage = null)
        {
            if (null == observer) return;

            observers.Add(observer);
            if(null != initMessage)
                observer.OnNext(initMessage);
        }

        public void Unregister(IObserver<string> observer)
        {
            observers.Remove(observer);
        }

        public void BroadcastMessage(string message)
        {
            if (observers.Count == 0)
            {
                Console.WriteLine(message);
                return;
            }

            foreach (var observer in observers)
                observer.OnNext(message);
        }

    }
}
