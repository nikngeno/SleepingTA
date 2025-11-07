// Program.cs
using System;
using System.Threading;
using System.Threading.Tasks;

public class SleepingTA
{

    // The number of waiting chairs in the hallway
    private const int NumChairs = 3;
    
    // The total number of students
    private const int NumStudents = 10;

    // 1. The Hallway Chairs
    // This semaphore represents the 3 waiting chairs.
    // Students must "acquire" a chair (Wait) to wait.
    // If no chairs are free, the "try" acquire (Wait(0)) will fail.
    private static SemaphoreSlim _hallwayChairs = new SemaphoreSlim(NumChairs, NumChairs);

    // 2. The TA's Office (Mutex)
    // A binary semaphore (acting as a mutex) to ensure only one student
    // is with the TA at a time.
    private static SemaphoreSlim _taMutex = new SemaphoreSlim(1, 1);

    // 3. The TA's "Doorbell"
    // The TA "sleeps" (Waits) on this semaphore.
    // A student "rings the bell" (Releases) to wake the TA.
    private static SemaphoreSlim _studentsReady = new SemaphoreSlim(0);

    // 4. The "Session Over" Signal
    // The TA signals (Releases) this when help is finished.
    // The student waits (Waits) on this before leaving the office.
    private static SemaphoreSlim _taDoneHelping = new SemaphoreSlim(0);

    // Used for simulating time with a bit of randomness
    // [ThreadStatic] gives each thread its own instance of Random
    // to avoid thread-safety issues.
    [ThreadStatic]
    private static Random? _random;
    private static Random Rng => _random ??= new Random(Thread.CurrentThread.ManagedThreadId);

    // --- Entry Point ---
    public static async Task Main(string[] args)
    {
        Console.WriteLine($"Starting simulation with {NumStudents} students and {NumChairs} chairs.");
        
        // Start the TA thread
        Task taTask = Task.Run(TaProcess);

        // Start all the student threads
        Task[] studentTasks = new Task[NumStudents];
        for (int i = 1; i <= NumStudents; i++)
        {
            studentTasks[i - 1] = Task.Run(() => StudentProcess(i));
            await Task.Delay(Rng.Next(1000, 3000)); // Stagger student arrivals
        }

        // Wait for all tasks to complete (in this case, they run forever,
        // so we'd need a CancellationToken for a real shutdown)
        await Task.WhenAll(studentTasks.Append(taTask));
    }

    // --- TA Thread Logic ---
    private static async Task TaProcess()
    {
        while (true) // The TA works for all office hours
        {
            SetConsoleColor(ConsoleColor.Cyan);
            Console.WriteLine("TA is napping... ZzzZzz...");

            // --- 1. Sleep ---
            // The TA waits for a student to "ring the bell"
            await _studentsReady.WaitAsync();

            // --- 2. Wake Up and Help ---
            // A student has "acquired" the TA mutex and signaled.
            SetConsoleColor(ConsoleColor.Cyan);
            Console.WriteLine("TA is awake and helping a student.");

            // Simulate helping by sleeping for a random time
            await Task.Delay(Rng.Next(3000, 6000));

            SetConsoleColor(ConsoleColor.Cyan);
            Console.WriteLine("TA is finished helping.");

            // --- 3. Signal Session Over ---
            // Tell the student they are free to go.
            _taDoneHelping.Release();
        }
    }

    // --- Student Thread Logic ---
    private static async Task StudentProcess(int id)
    {
        while (true) // Students keep programming and seeking help
        {
            // --- 1. Program ---
            SetConsoleColor(ConsoleColor.Green);
            Console.WriteLine($"Student {id} is programming.");
            await Task.Delay(Rng.Next(5000, 15000)); // Simulate programming

            // --- 2. Try to Get Help ---
            SetConsoleColor(ConsoleColor.Yellow);
            Console.WriteLine($"Student {id} needs help.");

            // Try to get a chair. Wait(0) is a non-blocking "try".
            if (_hallwayChairs.Wait(0))
            {
                // --- 3. Got a Chair ---
                SetConsoleColor(ConsoleColor.Yellow);
                Console.WriteLine($"Student {id} is waiting in the hallway. ({NumChairs - _hallwayChairs.CurrentCount}/{NumChairs} chairs full)");

                // --- 4. Wait for TA ---
                // Now, wait for the TA to be free.
                // This acquires the "office" mutex.
                await _taMutex.WaitAsync();

                // --- 5. See the TA ---
                // It's my turn! I leave the hallway chair.
                _hallwayChairs.Release();
                SetConsoleColor(ConsoleColor.Magenta);
                Console.WriteLine($"Student {id} is getting help from the TA.");
                
                // Ring the "doorbell" to wake the TA (or signal I'm ready)
                _studentsReady.Release();

                // Wait for the TA to signal the session is over
                await _taDoneHelping.WaitAsync();

                // --- 6. Leave Office ---
                // I'm done. I release the "office" mutex so the next
                // student (who is waiting) can enter.
                _taMutex.Release();
                
                SetConsoleColor(ConsoleColor.Green);
                Console.WriteLine($"Student {id} is done and leaving.");
            }
            else
            {
                // --- 7. No Chairs Available ---
                SetConsoleColor(ConsoleColor.Red);
                Console.WriteLine($"Student {id} found no chairs, will program instead.");
                // Loop back to programming
            }
        }
    }
    
    // Helper for coloring console output
    private static void SetConsoleColor(ConsoleColor color)
    {
        Console.ForegroundColor = color;
    }
}