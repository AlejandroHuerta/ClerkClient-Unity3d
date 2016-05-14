using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Clerk;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace ClerkTest {
    [TestClass]
    public class ManagerTimingTest {

        [TestMethod]
        public void ChangeDataImmediately() {
            var state = JObject.Parse(@"
            {
                worldTime: 560
            }");

            var stateManager = new Manager(state);
            
            var message = new StateMessage();
            message.action = StateMessage.Action.SET;
            message.key = "worldTime";
            message.value = "200";

            stateManager.Enqueue(message);

            stateManager.Update();

            Assert.AreEqual(200, stateManager.Get<int>("worldTime"));
        }

        [TestMethod]
        public void ChangeDataAfterTime() {
            var state = JObject.Parse(@"
            {
                worldTime: 560
            }");

            var stateManager = new Manager(state);

            var message = new StateMessage();
            message.action = StateMessage.Action.SET;
            message.key = "worldTime";
            message.value = "200";
            message.at = DateTime.Now.AddSeconds(1).ConvertToUnixTime();

            stateManager.Enqueue(message);

            stateManager.Update();

            Assert.AreEqual(560, stateManager.Get<int>("worldTime"));

            Thread.Sleep(1000);

            stateManager.Update();

            Assert.AreEqual(200, stateManager.Get<int>("worldTime"));
        }

        [TestMethod]
        public void EnqueueImmediateAfterDelayed() {
            var state = JObject.Parse(@"
            {
                worldTime: 560
            }");

            var stateManager = new Manager(state);

            var message = new StateMessage();
            message.action = StateMessage.Action.SET;
            message.key = "worldTime";
            message.value = "200";
            message.at = DateTime.Now.AddSeconds(1).ConvertToUnixTime();

            stateManager.Enqueue(message);

            message = new StateMessage();
            message.action = StateMessage.Action.SET;
            message.key = "worldTime";
            message.value = "300";
            message.at = null;

            stateManager.Enqueue(message);

            stateManager.Update();

            Assert.AreEqual(300, stateManager.Get<int>("worldTime"));

            Thread.Sleep(1000);

            stateManager.Update();

            Assert.AreEqual(200, stateManager.Get<int>("worldTime"));
        }
    }
}
