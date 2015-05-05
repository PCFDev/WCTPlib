﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WCTPlib.v1r1
{
    //enterprise
    public abstract class SubmitRequest : Operation
    {
        internal static SubmitRequest Parse(XElement operation)
        {
            if (operation == null)
                throw new ArgumentNullException("operation");

            var header = operation.Element("wctp-SubmitHeader");
            var payload = operation.Element("wctp-Payload");

            if (header == null || payload == null)
                return null;//throw?

            var response = payload.Elements().FirstOrDefault(_ => _.Name.LocalName == "wctp-MCR" || _.Name.LocalName == "wctp-Alphanumeric" || _.Name.LocalName == "wctp-TransparentData");
            var originator = header.Element("wctp-Originator");
            var recipient = header.Element("wctp-Recipient");
            var control = header.Element("wctp-MessageControl");

            if (response == null || originator == null || recipient == null || control == null)
                return null;//throw?

            SubmitRequest instance = null;
            switch (response.Name.LocalName)
            {
                case "wctp-MCR":
                    instance = new MCR(response);
                    break;
                case "wctp-Alphanumeric":
                    instance = new Alphanumeric(response);
                    break;
                case "wctp-TransparentData":
                    instance = new TransparentData(response);
                    break;
            }

            if (instance != null)
            {
                var submitTimestamp = (string)header.Attribute("submitTimestamp");
                var deliveryAfter = (string)control.Attribute("deliveryAfter");
                var deliveryBefore = (string)control.Attribute("deliveryBefore");

                instance.SubmitTimestamp = submitTimestamp == null ? DateTime.UtcNow : DateTime.Parse(submitTimestamp);//Can UtcNow be assumed here? probably not.
                instance.SenderId = (string)originator.Attribute("senderID");
                instance.SecurityCode = (string)originator.Attribute("securityCode");
                instance.MiscInfo = (string)originator.Attribute("miscInfo");
                instance.MessageId = (string)control.Attribute("messageID");
                instance.TransactionId = (string)control.Attribute("transactionID");
                instance.SendResponsesToId = (string)control.Attribute("sendResponsesToID");
                instance.AllowResponse = bool.Parse((string)control.Attribute("allowResponse") ?? "true");
                instance.NotifyWhenQueued = bool.Parse((string)control.Attribute("notifyWhenQueued") ?? "false");
                instance.NotifyWhenDelivered = bool.Parse((string)control.Attribute("notifyWhenDelivered") ?? "false");
                instance.NotifyWhenRead = bool.Parse((string)control.Attribute("notifyWhenRead") ?? "false");
                instance.DeliveryPriority = (Priority)Enum.Parse(typeof(Priority), (string)control.Attribute("deliveryPriority"), true);
                instance.DeliveryAfter = deliveryAfter == null ? default(DateTime?) : DateTime.Parse(deliveryAfter);
                instance.DeliveryBefore = deliveryBefore == null ? default(DateTime?) : DateTime.Parse(deliveryBefore);
                instance.Preformatted = bool.Parse((string)control.Attribute("preformatted") ?? "false");
                instance.AllowTruncation = bool.Parse((string)control.Attribute("allowTruncation") ?? "true");
                instance.RecipientId = (string)recipient.Attribute("recipientID");
                instance.AuthorizationCode = (string)recipient.Attribute("authorizationCode");
            }
            return instance;
        }

        #region Constructors

        private SubmitRequest()
        {
            SubmitTimestamp = DateTime.UtcNow;
            AllowResponse = true;
            AllowTruncation = true;
        }

        public SubmitRequest(string senderId, string recipientId, string messageId)
            : this()
        {
            if (String.IsNullOrEmpty(senderId))
                throw new ArgumentNullException("senderId");
            if (String.IsNullOrEmpty(recipientId))
                throw new ArgumentNullException("recipientId");
            if (String.IsNullOrEmpty(messageId))
                throw new ArgumentNullException("messageId");

            SenderId = senderId;
            RecipientId = recipientId;
            MessageId = messageId;
        }

        #endregion Constructors

        #region Properties

        public DateTime SubmitTimestamp { get; set; }//private setter?

        //Originator
        [Required, MaxLength(254)]
        public string SenderId { get; set; }
        //[DefaultValue(null)]
        public string SecurityCode { get; set; }
        //[DefaultValue(null)]
        public string MiscInfo { get; set; }

        //MessageControl
        [Required]
        public string MessageId { get; set; }
        //[DefaultValue(null)]
        public string TransactionId { get; set; }
        //[DefaultValue(null)]
        public string SendResponsesToId { get; set; }
        //[DefaultValue(true)]
        public bool AllowResponse { get; set; }
        //[DefaultValue(false)]
        public bool NotifyWhenQueued { get; set; }
        //[DefaultValue(false)]
        public bool NotifyWhenDelivered { get; set; }
        //[DefaultValue(false)]
        public bool NotifyWhenRead { get; set; }
        //[DefaultValue(0)]
        public Priority DeliveryPriority { get; set; }
        //[DefaultValue(null)]
        public DateTime? DeliveryAfter { get; set; }
        //[DefaultValue(null)]
        public DateTime? DeliveryBefore { get; set; }
        //[DefaultValue(false)]
        public bool Preformatted { get; set; }
        //[DefaultValue(true)]
        public bool AllowTruncation { get; set; }

        //Recipient
        [Required]
        public string RecipientId { get; set; }
        //[DefaultValue(null)]
        public string AuthorizationCode { get; set; }

        #endregion Properties

        #region Private Methods

        private XElement GetRecipient()
        {
            var element = new XElement("wctp-Recipient", new XAttribute("recipientID", RecipientId));
            if (!String.IsNullOrEmpty(AuthorizationCode))
                element.Add(new XAttribute("authorizationCode", AuthorizationCode));
            return element;
        }

        private XElement GetOriginator()
        {
            var element = new XElement("wctp-Originator", new XAttribute("senderID", SenderId));
            if (!String.IsNullOrEmpty(SecurityCode))
                element.Add(new XAttribute("securityCode", SecurityCode));
            if (!String.IsNullOrEmpty(MiscInfo))
                element.Add(new XAttribute("miscInfo", MiscInfo));
            return element;
        }

        private XElement GetMessageControl()
        {
            var element = new XElement("wctp-MessageControl", new XAttribute("messageID", MessageId));
            if (!String.IsNullOrEmpty(TransactionId))
                element.Add(new XAttribute("transactionID", TransactionId));
            if (!String.IsNullOrEmpty(SendResponsesToId))
                element.Add(new XAttribute("sendResponsesToID", SendResponsesToId));
            if (!AllowResponse)
                element.Add(new XAttribute("allowResponse", AllowResponse ? "true" : "false"));
            if (NotifyWhenQueued)
                element.Add(new XAttribute("notifyWhenQueued", NotifyWhenQueued ? "true" : "false"));
            if (NotifyWhenDelivered)
                element.Add(new XAttribute("notifyWhenDelivered", NotifyWhenDelivered ? "true" : "false"));
            if (NotifyWhenRead)
                element.Add(new XAttribute("notifyWhenRead", NotifyWhenRead ? "true" : "false"));
            if (DeliveryPriority != Priority.Normal)
                element.Add(new XAttribute("deliveryPriority", DeliveryPriority.ToString()));//ehh
            if (DeliveryAfter.HasValue)
                element.Add(new XAttribute("deliveryAfter", Timestamp(DeliveryAfter.Value)));
            if (DeliveryBefore.HasValue)
                element.Add(new XAttribute("deliveryBefore", Timestamp(DeliveryBefore.Value)));
            if (Preformatted)
                element.Add(new XAttribute("preformatted", Preformatted ? "true" : "false"));
            if (!AllowTruncation)
                element.Add(new XAttribute("allowTruncation", AllowTruncation ? "true" : "false"));
            return element;
        }

        private XElement GetSubmitHeader()
        {
            return new XElement("wctp-SubmitHeader", new XAttribute("submitTimestamp", Timestamp(SubmitTimestamp)),
                                GetOriginator(),
                                GetMessageControl(),
                                GetRecipient());
        }

        #endregion Private Methods

        internal abstract XElement GetPayload();

        #region Overrides

        protected override XElement GetOperation()
        {
            return new XElement("wctp-SubmitRequest", GetSubmitHeader(), GetPayload());
        }

        #endregion Overrides

        public class MCR : SubmitRequest
        {
            internal MCR(XElement response)
            {
                throw new NotImplementedException();
            }

            public MCR(string senderId, string recipientId, string messageId)
                : base(senderId, recipientId, messageId)
            {
                Choices = new List<string>();
            }

            public string Message { get; set; }
            public IList<string> Choices { get; set; }

            internal override XElement GetPayload()
            {
                var payload = new XElement("wctp-MCR", new XElement("wctp-MessageText", Message));
                foreach (var choice in Choices)
                {
                    payload.Add(new XElement("wctp-Choice", choice));
                }
                return new XElement("wctp-Payload", payload);
            }
        }

        public class TransparentData : SubmitRequest
        {
            internal TransparentData(XElement response)
            {
                Type = (DataType)Enum.Parse(typeof(DataType), (string)response.Attribute("type"), true);
                Encoding = (DataEncoding)Enum.Parse(typeof(DataEncoding), (string)response.Attribute("encoding"), true);
                Data = Decode(response.Value, Encoding);//Do this through a setter on Message?
            }

            public TransparentData(string senderId, string recipientId, string messageId)
                : base(senderId, recipientId, messageId)
            {
                Type = DataType.OPAQUE;
                Encoding = DataEncoding.base64;
            }

            public DataType Type { get; set; }
            public DataEncoding Encoding { get; set; }
            public byte[] Data { get; set; }

            public string Message
            {
                get
                {
                    return Encode(Data, Encoding);
                }
            }

            internal override XElement GetPayload()
            {
                return new XElement("wctp-Payload", new XElement("wctp-TransparentData",
                                                                    new XAttribute("type", Type.ToString()),
                                                                    new XAttribute("encoding", Encoding.ToString()),
                                                                    Message));
            }
        }

        public class Alphanumeric : SubmitRequest
        {
            internal Alphanumeric(XElement response)
            {
                Message = response.Value;
            }

            public Alphanumeric(string senderId, string recipientId, string messageId)
                : base(senderId, recipientId, messageId)
            {
            }

            public string Message { get; set; }

            internal override XElement GetPayload()
            {
                return new XElement("wctp-Payload", new XElement("wctp-Alphanumeric", Message));
            }
        }
    }
}
