using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Amazon.LambdaPowertools.Tracing;

namespace AWS.Lambda.PowerTools.Tracing.Tests
{
   
    public class TestFunction
    {
        private readonly ITracer _tracer;

        public TestFunction()
        {
            _tracer = new Tracer();
        }
        
        [CaptureMethod(service: "MyService", disabled:false, autoPatch: false, patchmodules: null)]
        private string ConfirmBooking(string bookingId)
        {
            var response =  AddConfirmation(bookingId);
            
            _tracer.PutMetadata("BookingConfirmation", response["requestId"]);
            _tracer.PutMetadata("Booking confirmation", "{}");
            _tracer.PutMetadata("", "");
            
            
            return response.ToString();
        }

        private Dictionary<string,string> AddConfirmation(string bookingId)
        {
            throw new NotImplementedException();
        }
    }
}