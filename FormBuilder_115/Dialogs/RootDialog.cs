using System;
using System.Threading.Tasks;
using FormBuilder_115.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

namespace FormBuilder_115.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            // Calculate something for us to return
            int length = (activity.Text ?? string.Empty).Length;

            // Return our reply to the user
            await context.PostAsync($"You sent {activity.Text} which was {length} characters");

            FormBuilderTestModel test = new FormBuilderTestModel();

            var formDialog = new FormDialog<FormBuilderTestModel>(test, test.BuildForm);

            context.Call(formDialog, this.ResumeOperation);
        }

        private async Task ResumeOperation(IDialogContext context, IAwaitable<FormBuilderTestModel> result)
        {
            try
            {
                var response = await result;

                await context.SayAsync($"Test Sucess! {JsonConvert.SerializeObject(response)}");

                context.Done(true);

            }
            catch (Exception e)
            {
                await context.SayAsync($"Error: {JsonConvert.SerializeObject(e)}");

                context.Done(false);
            }
        }
    }
}