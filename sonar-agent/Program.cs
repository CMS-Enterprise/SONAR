// See https://aka.ms/new-console-template for more information

using System;
using System.Net.Http;
using Cms.BatCave.Sonar.Agent;

Console.WriteLine("Hello, World!");

var client = new SonarClient(baseUrl: "http://localhost:8081/", new HttpClient());

await client.ReadyAsync();
