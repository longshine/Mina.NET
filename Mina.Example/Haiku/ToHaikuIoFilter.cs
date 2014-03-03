using System;
using System.Collections.Generic;
using Mina.Core.Filterchain;
using Mina.Core.Session;

namespace Mina.Example.Haiku
{
    class ToHaikuIoFilter : IoFilterAdapter
    {
        public override void MessageReceived(INextFilter nextFilter, IoSession session, object message)
        {
            List<String> phrases = session.GetAttribute<List<String>>("phrases");

            if (null == phrases)
            {
                phrases = new List<String>();
                session.SetAttribute("phrases", phrases);
            }

            phrases.Add((String)message);

            if (phrases.Count == 3)
            {
                session.RemoveAttribute("phrases");

                base.MessageReceived(nextFilter, session, new Haiku(phrases
                        .ToArray()));
            }
        }
    }
}
