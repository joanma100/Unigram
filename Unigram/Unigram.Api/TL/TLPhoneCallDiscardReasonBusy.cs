// <auto-generated/>
using System;

namespace Telegram.Api.TL
{
	public partial class TLPhoneCallDiscardReasonBusy : TLPhoneCallDiscardReasonBase 
	{
		public TLPhoneCallDiscardReasonBusy() { }
		public TLPhoneCallDiscardReasonBusy(TLBinaryReader from)
		{
			Read(from);
		}

		public override TLType TypeId { get { return TLType.PhoneCallDiscardReasonBusy; } }

		public override void Read(TLBinaryReader from)
		{
		}

		public override void Write(TLBinaryWriter to)
		{
			to.Write(0xFAF7E8C9);
		}
	}
}