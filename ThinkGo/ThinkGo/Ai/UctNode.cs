namespace ThinkGo.Ai
{
    using System.Diagnostics;

    class UctNode
    {
        private MeanTracker mean = new MeanTracker();
        private MeanTracker rave = new MeanTracker();

        public UctNode(MoveInfo moveInfo)
        {
            this.Move = moveInfo.Point;
            this.mean.Initialize(moveInfo.Value, moveInfo.Count);
            this.rave.Initialize(moveInfo.Value, moveInfo.Count);
			this.HasBeenVisited = false;
        }

        public int Move { get; private set; }
        public UctNode FirstChild { get; set; }
        public UctNode Next { get; set; }
        public int NumChildren { get; set; }
        public int PosCount { get; set; }

        public bool HasMean { get { return this.mean.IsDefined; } }
        public int MoveCount { get { return (int)(this.mean.Count + 0.5f); } }
        public float Mean { get { return this.mean.Mean; } }

        public bool HasRaveValue { get { return this.rave.IsDefined; } }
        public float RaveCount { get { return this.rave.Count; } }
        public float RaveValue { get { return this.rave.Mean; } }

		public bool HasBeenVisited { get; private set; }

        public bool IsProven
        {
            get { return false; /* TODO! */ }
        }

        public void AddGameResult(float eval)
        {
            this.mean.Add(eval);
			this.HasBeenVisited = true;
        }

        public void AddRaveValue(float eval, float weight)
        {
            this.rave.Add(eval, weight);
        }
    }

    struct MeanTracker
    {
        private float count;
        private float mean;

        public bool IsDefined
        {
            get { return this.count != 0.0f; }
        }

        public float Mean
        {
            get { return this.mean; }
        }

        public float Count
        {
            get { return this.count; }
        }

        public void Initialize(float value, float count)
        {
            this.mean = value;
            this.count = count;
        }

        public void Add(float value)
        {
            Debug.Assert(value > -1e-6 && value < 1.000001);

            float count = this.count;
            ++count;
            this.mean += (value - this.mean) / count;
            this.count = count;
            
            Debug.Assert(!double.IsNaN(this.mean));
        }

        public void Remove(float value)
        {
            if (this.count > 0.9999)
            {
                float count = this.count;
                --count;
                this.mean += (this.mean - value) / count;
                this.count = count;
            }
            else
            {
                this.count = 0.0f;
                this.mean = 0.0f;
            }

            Debug.Assert(!double.IsNaN(this.mean));
        }

        public void Add(float value, float n)
        {
            float count = this.count;
            count += n;
            this.mean += n * (value - this.mean) / count;
            this.count = count;

            Debug.Assert(!double.IsNaN(this.mean));
        }

        public void Remove(float value, float n)
        {
            if (this.count > n - 1e-6)
            {
                float count = this.count;
                this.count -= n;
                this.mean += n * (this.mean - value) / count;
                this.count = count;
            }
            else
            {
                this.mean = 0.0f;
                this.count = 0.0f;
            }

            Debug.Assert(!double.IsNaN(this.mean));
        }
    }

    public struct MoveInfo
    {
        public int Point;
        public float Value;
        public int Count;

        public MoveInfo(int p)
        {
            this.Point = p;
            this.Value = 0.0f;
            this.Count = 0;
        }
    }
}
