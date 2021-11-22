using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Utilities
{
    public class ChainQueue<K, T>
    {
        private readonly object locker = new object();
        public int Count = 0;
        public bool IsEmpty = true;
        private ChainElement<K, T> last;
        private ChainElement<K, T> head;
        private ConcurrentDictionary<K, ChainElement<K, T>> elements = new ConcurrentDictionary<K, ChainElement<K, T>>();
        public ChainQueue()
        {

        }

        public void Enqueue(K key, T element)
        {
            lock (locker)
            {
                ChainElement<K, T> chainElement = new ChainElement<K, T>(key, element, DateTimeOffset.Now.ToUnixTimeMilliseconds(), this.Count);

                chainElement.AttachToTheEndOfChain(this.last);
                this.last = chainElement;
                if (this.head == null)
                {
                    this.head = chainElement;
                }
                this.elements.TryAdd(chainElement.key, chainElement);
                this.Count++;
                this.IsEmpty = false;
            }
        }

        public bool Dequeue([MaybeNullWhen(false)] out T value)
        {
            lock (locker)
            {
                ChainElement<K, T> element = this.head;

                if (element == null)
                {
                    value = default(T);
                    return false;
                }
                element.Remove();
                this.head = element.previous;
                if (element.key.Equals(this.last.key))
                {
                    this.last = null;
                }
                if (this.head == null)
                {
                    this.IsEmpty = true;
                }
                this.elements.TryRemove(element.key, out _);
                this.Count--;
                value = element.value;
                return true;
            }
        }

        public int GetPosition(K key)
        {
            ChainElement<K, T> element;

            if (this.elements.TryGetValue(key, out element))
            {
                element.time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                return element.position;
            }
            return -1;
        }

        public void RemoveTimeOut(long timeOutMilliseconds, Action<K> removedEvent)
        {
            Func<ChainElement<K, T>, int, bool> actionRemoveTimeOutMilliseconds = (e, i) =>
            {
                if (e.time + timeOutMilliseconds < DateTimeOffset.Now.ToUnixTimeMilliseconds())
                {
                    e.Remove();
                    this.Count--;
                    return true;
                }
                return false;
            };
            Action<ChainElement<K, T>> cleanHeadAndLast = (e) =>
            {
                if (this.head.key.Equals(e.key))
                {
                    if (e.previous == null)
                    {
                        this.head = null;
                        this.IsEmpty = true;
                    }
                    else
                    {
                        this.head = e.previous;
                    }
                }
                if (this.last.key.Equals(e.key))
                {
                    if (e.next == null)
                    {
                        this.last = null;
                    }
                    else
                    {
                        this.last = e.next;
                    }
                }
            };
            this.ApplyAction(actionRemoveTimeOutMilliseconds, cleanHeadAndLast, removedEvent);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ChainElement<K, T> el = this.head;
            int i = 0;
            while (el != null)
            {
                sb.AppendLine($"Element {i} {el.key}");
                el = el.previous;
                i++;
            }
            return sb.ToString();
        }

        private void ApplyAction(Func<ChainElement<K, T>, int, bool> action, Action<ChainElement<K, T>> cleanHeadAndLast, Action<K> removedEvent)
        {
            lock (locker)
            {
                ChainElement<K, T> element = this.head;
                int i = 0;

                while (element != null)
                {
                    if (action(element, i))
                    {
                        cleanHeadAndLast(element);
                        removedEvent(element.key);
                        this.elements.TryRemove(element.key, out _);
                    }
                    element = element.previous;
                    i++;
                }
            }
        }
    }

    public class ChainElement<K, T>
    {
        public ChainElement<K, T> previous = null;
        public ChainElement<K, T> next = null;
        public K key;
        public T value;
        public long time;
        public int position;
        public ChainElement(K key, T value, long time, int position)
        {
            this.key = key;
            this.value = value;
            this.time = time;
            this.position = position;
        }

        public void Remove()
        {
            if (this.previous != null)
            {
                this.previous.next = this.next;
            }
            if (this.next != null)
            {
                this.next.previous = this.previous;
            }

            // update position of previous:
            ChainElement<K, T> el = this;
            int position = this.position;
            int i = 0;
            while (el.previous != null)
            {
                el = el.previous;
                el.position = position + i;
                i++;
            }
        }

        public void AttachToTheEndOfChain(ChainElement<K, T> last)
        {
            if (last != null)
            {
                last.previous = this;
                this.next = last;
            }
        }
    }
}
