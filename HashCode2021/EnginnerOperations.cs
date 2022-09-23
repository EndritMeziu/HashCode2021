using HashCode2021.Input;

namespace HashCode2021
{
    public class EnginnerOperation
    {
        public int? BinaryId { get; set; }
        public string Operation { get; set; }
        public string FeatureName { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }


        public EnginnerOperation Clone()
        {
            return new EnginnerOperation
            {
                BinaryId = this.BinaryId,
                StartTime = this.StartTime,
                EndTime = this.EndTime,
                FeatureName = this.FeatureName,
                Operation = this.Operation
            };
        }
    }
}
