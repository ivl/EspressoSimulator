using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace EspressoSimulator
{
    // class that holds all future events 
    // I was surprised that C# .NET has'nt ready priority queue. So also was implemented simple priority queue
   
    class EventsTimeline
    {
        int eventsNum = 0;
        // Helper class that holds action and timestamp of the event
        class Itm: IComparable 
        {
            public int time;
            public Func<int, String> action;
            public int eventNum;

            public Itm(int _time, Func<int, String> evnt, int _eventNum)
            {
                time = _time;
                action = evnt;
                eventNum = _eventNum;
            }

            public int CompareTo(object o)
            {
                Itm that = (Itm) o;
                if (this.eventNum > that.eventNum)
                {
                    return -1;
                }

                if (this.eventNum == that.eventNum)
                {
                    return 0;
                }

                return 1;
            }
        }
        
        PriorityQueue<Itm> eventsQueue;
        
        public EventsTimeline()
        {
            eventsQueue = new PriorityQueue<Itm>();
        }
        // function that post new event to the queue. Priority is time when event will happend
        public void postEventToTimeline(int seconds, Func<int, String> futureEvent)
        {
            Itm itm = new Itm(seconds, futureEvent, eventsNum ++);
            eventsQueue.Add(seconds, itm);
        }

        // fucntion that step thru simulation
        // return action description
        public String doSimulationStep()
        {
            if(eventsQueue.Count > 0)
            {
                Itm itm = eventsQueue.RemoveMin();
                int time = itm.time;
                return itm.action(time);
            }
            return "";
        }
    }
    // Class that responsible for coffe machine implementation
    class CoffeMachine
    {
        EventsTimeline eventsTimeline;
        Boolean isServingMode = false;
        int coffeBrewingTime;
        Dictionary<Employee, int> dict;

        // Method for adding enginiier to queue
        public void getQueued(Employee empl, int seconds)
        {
            dict.Add(empl, seconds);
            pushStartButton(seconds);
        }
        public CoffeMachine(EventsTimeline timeline, int brewingTime)
        {
            eventsTimeline = timeline;
            coffeBrewingTime = brewingTime;
            dict = new Dictionary<Employee, int>();
        }

        // Action when somebody from queue starts brewing coffe
        public void pushStartButton(int seconds)
        {
            if (!isServingMode && dict.Count > 0)
            {
                isServingMode = true;
                eventsTimeline.postEventToTimeline(seconds + coffeBrewingTime, pourTheCoffe);
            }
        }

        // Action when coffemachine prepared coffe and ready to fill glass
        // In this method we are looking for employee in queue with smallest time
        // In case if in queue present superbusy user - we select superuser for serving
        public String pourTheCoffe(int seconds)
        {
            isServingMode = false;
            String salt = "";
            Employee empl = dict.Aggregate((l,r) => l.Value < r.Value ? l : r).Key;
            var superBusyEmpl = dict.Where(pair => pair.Key.isSuperBusyMode == true).Select(pair => pair);
            if (superBusyEmpl != null && superBusyEmpl.ToList().Count > 0)
            {
                var winner = superBusyEmpl.Aggregate((l, r) => l.Value < r.Value ? l : r);
                empl = winner.Key;
                salt = String.Format(" served as superbusy emploee with time {0}", winner.Value);
            }
            empl.doStartWork(seconds);
            dict.Remove(empl);
            pushStartButton(seconds);
            return String.Format("Coffemachine prepared coffe for {0} at {1}{2}", empl.employeeName, seconds, salt);
        }
    }
    // Class that describes Employee
    class Employee
    {
        public String employeeName;
        CoffeMachine espressoMachine;
        EventsTimeline eventsTimeline;
        
        // Lambdas to fucntions
        Func<int> inWorkingModeTime;
        Func<int> inNotBusyModeTime;
        Func<int> inBusyModeTime;

        public Boolean isSuperBusyMode = false;


        public Employee(String name, EventsTimeline timeline, CoffeMachine coffeMachine, Func<int> workingTime, Func<int> notBusyModeTime, Func<int> busyModeTime)
        {
            employeeName = name;
            eventsTimeline = timeline;
            espressoMachine = coffeMachine;
            inWorkingModeTime = workingTime;
            inNotBusyModeTime = notBusyModeTime;
            inBusyModeTime = busyModeTime;

            doBecomeNotBusyMode(0);
            doStartWork(0);


        }
        // Action when user become regular busy mode. At the beginning of work or after superbusy mode
        public String doBecomeNotBusyMode(int seconds)
        {
            isSuperBusyMode = false;
            eventsTimeline.postEventToTimeline(seconds + inNotBusyModeTime(), doBecomeSuperBusyMode);
            return String.Format("Employee {0} has entered to not busy mode at time {1}", employeeName, seconds);
        }
        // Action when user become superbusy mode after some time of working in regular mode
        public String doBecomeSuperBusyMode(int seconds)
        {
            isSuperBusyMode = true;
            eventsTimeline.postEventToTimeline(seconds + inBusyModeTime(), doBecomeNotBusyMode);
            return String.Format("Employee {0} has entered to super busy mode at time {1}", employeeName, seconds);
        }
        // Action that fired when user worked for some amount of time
        public String doCoffebreak(int seconds)
        {
            espressoMachine.getQueued(this, seconds);
            return String.Format("Employee {0} has went for coffe at time {1}", employeeName, seconds);
        }
        // Method that enter user to work mode after coffebreake or at the beginning
        public void doStartWork(int seconds)
        {
            eventsTimeline.postEventToTimeline(seconds + inWorkingModeTime(), doCoffebreak);
        }
    }
   
    // Helper classes that describes min-heap and Priority Heap. Alas in .NET its missed. 
    class MinHeap<T> where T : IComparable<T>
    {
        private List<T> array = new List<T>();

        public void Add(T element)
        {
            array.Add(element);
            int c = array.Count - 1;
            while (c > 0 && array[c].CompareTo(array[c / 2]) == -1)
            {
                T tmp = array[c];
                array[c] = array[c / 2];
                array[c / 2] = tmp;
                c = c / 2;
            }
        }

        public T RemoveMin()
        {
            T ret = array[0];
            array[0] = array[array.Count - 1];
            array.RemoveAt(array.Count - 1);

            int c = 0;
            while (c < array.Count)
            {
                int min = c;
                if (2 * c + 1 < array.Count && array[2 * c + 1].CompareTo(array[min]) == -1)
                    min = 2 * c + 1;
                if (2 * c + 2 < array.Count && array[2 * c + 2].CompareTo(array[min]) == -1)
                    min = 2 * c + 2;

                if (min == c)
                    break;
                else
                {
                    T tmp = array[c];
                    array[c] = array[min];
                    array[min] = tmp;
                    c = min;
                }
            }

            return ret;
        }

        public T Peek()
        {
            return array[0];
        }

        public int Count
        {
            get
            {
                return array.Count;
            }
        }
    }

    class PriorityQueue<T>
    {
        internal class Node : IComparable<Node>
        {
            public int Priority;
            public T O;
            public int CompareTo(Node other)
            {
                return Priority.CompareTo(other.Priority);
            }
        }

        private MinHeap<Node> minHeap = new MinHeap<Node>();

        public void Add(int priority, T element)
        {
            minHeap.Add(new Node() { Priority = priority, O = element });
        }

        public T RemoveMin()
        {
            return minHeap.RemoveMin().O;
        }

        public T Peek()
        {
            return minHeap.Peek().O;
        }

        public int Count
        {
            get
            {
                return minHeap.Count;
            }
        }
    }

    // My realisation of exponential distribution
    class IVLRand
    {
        static public double nextRandomExp(double rate)
        {
            Random rnd = new Random();

            return (-Math.Log10(1.0f - rnd.NextDouble()) / rate);
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
    
            

            int simulationEmployeeNum = 20; //  default value
            int simulationBrewingTime = 30; // 30 seconds default value for brewing time
            int simulationTimeInBusyMode = 60 * 60 * 2; // 2 hours default value
            double simulationProbabilityToBecomeSuperBusy = 0.2; // default value

            List<Employee> currentEmploeeList = new List<Employee>();

            while(true)
            {
              Console.WriteLine("Please spicify number of employees (or just enter empty for default value {0}) :=>", simulationEmployeeNum);
              string readline = Console.ReadLine();
              int value;
              if (readline == "")
              {
                  break;
              }
              if (int.TryParse(readline, out value) && value > 0)
              {
                  simulationEmployeeNum = value;
                  break;
              }
              else
              {
                  Console.WriteLine("Please enter correct value");
              }
            }

            while (true)
            {
                Console.WriteLine("Please spicify coffe breewing time in seconds (default value {0}) :=>", simulationBrewingTime);
                string readline = Console.ReadLine();
                int value;
                if (readline == "")
                {
                    break;
                }
                if (int.TryParse(readline, out value) && value > 0)
                {
                    simulationBrewingTime = value;
                    break;
                }
                else
                {
                    Console.WriteLine("Please enter correct value");
                }
            }

            while (true)
            {
                Console.WriteLine("Please spicify time in busy mode (or just enter empty for default value {0}) :=>", simulationTimeInBusyMode);
                string readline = Console.ReadLine();
                int value;
                if (readline == "")
                {
                    break;
                }
                if (int.TryParse(readline, out value) && value > 0)
                {
                    simulationTimeInBusyMode = value;
                    break;
                }
                else
                {
                    Console.WriteLine("Please enter correct value");
                }
            }

            while (true)
            {
                Console.WriteLine("Please spicify probability become superbusy (or just enter empty for default value {0}) :=>", simulationProbabilityToBecomeSuperBusy);
                string readline = Console.ReadLine();
                double value;
                if (readline == "")
                {
                    break;
                }
                if (double.TryParse(readline, out value) && value >= 0.0)
                {
                    simulationProbabilityToBecomeSuperBusy = value;
                    break;
                }
                else
                {
                    Console.WriteLine("Please enter correct value");
                }
            }

            var simulationEventsTimeline = new EventsTimeline();
            var simulationCoffeMachine = new CoffeMachine(simulationEventsTimeline, simulationBrewingTime);

            for (int i = 0; i < simulationEmployeeNum; i ++ )
            {
                Func<int> workingTime = () =>
                {
                    double rnd = IVLRand.nextRandomExp(1.0 / 60.0*60.0) *1000;
                    int ret = Convert.ToInt32(rnd);
                    return ret;
                };

                Func<int> inNonBusyMode = () =>
                {
                    double rnd = IVLRand.nextRandomExp(0.2) * 1000;
                    int ret = Convert.ToInt32(rnd);
                    return ret;
                };

                Func<int> inBusyMode = () =>
                {

                    return simulationTimeInBusyMode;
                };

                
                //Console.WriteLine("Generated random {0}", workingTime);
                var empl = new Employee(i.ToString(), simulationEventsTimeline, simulationCoffeMachine, workingTime, inNonBusyMode, inBusyMode);
                currentEmploeeList.Add(empl);
                Thread.Sleep(50); // added to prevent filling by same numbers on very fast machine
            }

            // also can be entered in the beginning num of the simulation iteration
            for (int i = 0; i < 2000; i ++)
            {
                var res = simulationEventsTimeline.doSimulationStep();
                if (res != "")
                {
                    Console.WriteLine(res);
                }
            }
                Console.ReadKey();
        }
    }
}
