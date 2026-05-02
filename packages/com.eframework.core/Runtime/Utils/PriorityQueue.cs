using System.Collections.Generic;

namespace EFrame.Runtime.Utils
{
    public class PriorityQueue<T>
    {
        private readonly List<(T item, float priority)> elements = new List<(T item, float priority)>();

        public int Count
        {
            get => elements.Count;
        }

        public void Clear()
        {
            elements.Clear();
        }

        public void Enqueue(T item, float priority)
        {
            elements.Add((item, priority));
            HeapifyUp(elements.Count - 1);
        }

        public T Dequeue()
        {
            if (elements.Count == 0) return default(T);
            T item = elements[0].item;
            elements[0] = elements[^1];
            elements.RemoveAt(elements.Count - 1);
            HeapifyDown(0);
            return item;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (elements[index].priority >= elements[parent].priority) break;
                (elements[index], elements[parent]) = (elements[parent], elements[index]);
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            int lastIndex = elements.Count - 1;
            while (true)
            {
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                int smallest = index;

                if (leftChild <= lastIndex && elements[leftChild].priority < elements[smallest].priority)
                    smallest = leftChild;

                if (rightChild <= lastIndex && elements[rightChild].priority < elements[smallest].priority)
                    smallest = rightChild;

                if (smallest == index) break;

                (elements[index], elements[smallest]) = (elements[smallest], elements[index]);
                index = smallest;
            }
        }
    }
}
