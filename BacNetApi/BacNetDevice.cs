namespace BacNetApi
{
    public class BacNetDevice 
    {
        public int Id { get; private set; }
        public BacNetObjectIndexer Objects { get; set; }
        private BacNet _network;

        public BacNetDevice(int id, BacNet network)
        {
            Id = id;
            _network = network;
            Objects = new BacNetObjectIndexer(this);
        }        
    }
}