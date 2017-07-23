// <auto-generated/>
using System;

namespace Telegram.Api.TL
{
	public partial class TLInputMediaGeoPoint : TLInputMediaBase 
	{
		public TLInputGeoPointBase GeoPoint { get; set; }

		public TLInputMediaGeoPoint() { }
		public TLInputMediaGeoPoint(TLBinaryReader from)
		{
			Read(from);
		}

		public override TLType TypeId { get { return TLType.InputMediaGeoPoint; } }

		public override void Read(TLBinaryReader from)
		{
			GeoPoint = TLFactory.Read<TLInputGeoPointBase>(from);
		}

		public override void Write(TLBinaryWriter to)
		{
			to.Write(0xF9C44144);
			to.WriteObject(GeoPoint);
		}
	}
}