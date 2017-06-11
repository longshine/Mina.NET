Mina.NET
========

.NET implementation of [Apache MINA](http://mina.apache.org/). I like the ideas in it, simple yet functional, but I failed to find one in .NET, finally I created one.

Mina.NET is a network application framework which helps users develop high performance and high scalability network applications easily. It provides an abstract event-driven asynchronous API over various transports such as TCP/IP via **async socket**.

Mina.NET is often called:

* NIO framework library,
* client server framework library, or
* a networking socket library

Features
-----------

Mina.NET is a simple yet full-featured network application framework which provides:

* Unified API for various transport types:
  - TCP/IP & UDP/IP via .NET asynchronous socket
  - Serial communication (RS232)
  - Loopback (in-application pipe) communication
  - You can implement your own!
* Filter interface as an extension point;
* Low-level and high-level API:
  - Low-level: uses IoBuffers
  - High-level: uses user-defined message objects and codecs
* Highly customizable thread model:
  - Single thread
  - One thread pool
  - More than one thread pools
* Out-of-the-box SSL · TLS
* Overload shielding & traffic throttling
* Stream-based I/O support via StreamIoHandler

Quick Start
-----------

```csharp
  IoAcceptor acceptor = new AsyncSocketAcceptor();

  acceptor.FilterChain.AddLast("logger", new LoggingFilter());
  acceptor.FilterChain.AddLast("codec", new ProtocolCodecFilter(new TextLineCodecFactory(Encoding.UTF8)));

  acceptor.ExceptionCaught += (o, e) => Console.WriteLine(e.Exception);
  
  acceptor.SessionIdle += (o, e) => Console.WriteLine("IDLE " + e.Session.GetIdleCount(e.IdleStatus));
  
  acceptor.MessageReceived += (o, e) =>
  {
    String str = e.Message.ToString();

    // "Quit" ? let's get out ...
    if (str.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
      e.Session.Close(true);
      return;
    }

    // Send the current date back to the client
    e.Session.Write(DateTime.Now.ToString());
    Console.WriteLine("Message written...");
  };
  
  acceptor.Bind(new IPEndPoint(IPAddress.Any, 8080));
```

See https://mina.codeplex.com/documentation for more.

License
-------

Licensed under the Apache License, Version 2.0. You may obtain a copy of the License at [LICENSE](LICENSE) or http://www.apache.org/licenses/LICENSE-2.0.
