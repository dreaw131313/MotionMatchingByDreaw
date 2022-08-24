namespace MotionMatching.Tools
{
    public class IDCreator
    {

        int start;
        int id;

        public IDCreator(int start = 0)
        {
            this.start = start;
            id = start;
        }

        public void IDStart(int start)
        {
            id = start;
        }

        public int nextID()
        {
            id++;
            return id;
        }
    }
}