using System;

namespace EFrameWork.Runtime.DataStorage
{
    // 继承 DataTable，定义具体数据表
    [Serializable]
    public class TestDataModal
    {
        public string Name;
        public int Level;
    }

    public class TestDataTable : DataTable<TestDataModal>
    {
        // 重写 GetDefaultData 方法，提供默认值

        protected override TestDataModal GetDefaultData()
        {
            return new TestDataModal
            {
                Name = "defaultName",
                Level = 1
            };
        }

        public void Test()
        {
            //实例化时会自动加载数据

            DataManager dataManager = new(new JsonFileStorage());
            dataManager.RegisterTable<TestDataTable>();

            TestDataTable table = dataManager.GetTable<TestDataTable>();
            table.Data.Name = "ethan";
            table.Save(true);
            table.Reset();
            dataManager.LoadAll();
            dataManager.SaveAll(true);
        }
    }
}
