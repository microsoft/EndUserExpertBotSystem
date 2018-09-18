// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;
using ModernApps.CommunitiyBot.Common.Providers;
using ModernApps.CommunityBot.BotCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ModernApps.CommunityBot.ExpertsBot.Dialogs
{
    [Serializable]
    public class WrongAnswerDialog : ExpertDialogBase
    {
        private string question;
        private string originalQuestion;
        private string originalAnswer;

        public WrongAnswerDialog(string question, string originalQuestion, string originalAnswer) : base()
        {
            this.question = question;
            this.originalAnswer = originalAnswer;
            this.originalQuestion = originalQuestion;
        }


        public override async Task OnStart(IDialogContext context)
        {
            var requiredBehavior = context.ConversationData.GetValue<string>("requiredBehavior");

            if (requiredBehavior == "KeepNew" || requiredBehavior == "KeepBoth")
            {
                PromptDialog.Text(context, AfterAnswer, string.Format(messageProvider.GetMessage("askForAnswer"), question));
            }
            else
            {
                await context.PostAsync(messageProvider.GetMessage("KeepingOriginal"));
                await SendAnswerToUsers(context, question, originalAnswer);
                context.Done(new DialogResult());
            }
        }

     
        private async Task AfterAnswer(IDialogContext context, IAwaitable<string> result)
        {
            var answer = await result;

            var requiredBehavior = context.ConversationData.GetValue<string>("requiredBehavior");

            if (requiredBehavior == "KeepNew")
            {
                var qnaResponse = await qnaMakerProvider.GetQandAResponse(question);
                await KeepNewAnswer(context, question, answer, qnaResponse);
            }
            else if (requiredBehavior == "KeepBoth")
            {
                await context.PostAsync(messageProvider.GetMessage("KeepingBoth"));
                await qnaMakerProvider.StoreNewAnswer(question, answer);
                await SendAnswerToUsers(context, question, answer);
            }
            
            context.Done(new DialogResult());
        }


    }
}