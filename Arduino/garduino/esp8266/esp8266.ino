#include <ESP8266WiFi.h>
#include <ESP8266WiFiAP.h>
#include <ESP8266WiFiGeneric.h>
#include <ESP8266WiFiMulti.h>
#include <ESP8266WiFiScan.h>
#include <ESP8266WiFiSTA.h>
#include <ESP8266WiFiType.h>
#include <WiFiClient.h>
#include <WiFiClientSecure.h>
#include <WiFiServer.h>
#include <WiFiUdp.h>

#include <SimpleDHT.h>

// for DHT11, 
//      VCC: 5V or 3V
//      GND: GND
//      DATA: 2
int pinDHT11 = 0;
SimpleDHT11 dht11;

const char* ssid = "wifi-ssid";
const char* password = "password";

const char* emonHost = "emonhost";
const char* emonKey = "emonkey";

// addresses of interest - might be better to use DHCP here if possible
IPAddress staticIP(192,168,1,110);
IPAddress gateway(192,168,1,1);
IPAddress subnet(255,255,255,0);

WiFiClient client;

void setup() {
  Serial.begin(115200);

  // Connecting to WiFi network
  Serial.println();
  Serial.print("Connecting to ");
  Serial.println(ssid);

  WiFi.config(staticIP, gateway, subnet);
  WiFi.begin(ssid, password);
  
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("");
  Serial.print("WiFi connected: ");
  Serial.println(WiFi.localIP());

}

void loop() {
  // start working...
  Serial.println("=================================");
  Serial.println("Sample DHT11...");
  
  // read without samples.
  byte temperature = 0;
  byte humidity = 0;
  if (dht11.read(pinDHT11, &temperature, &humidity, NULL)) {
    Serial.print("Read DHT11 failed.");
    delay(1000);
    return;
  }
  
  Serial.print("Sample OK: ");
  Serial.print((int)temperature); Serial.print(" *C, "); 
  Serial.print((int)humidity); Serial.println(" %");

  // Upload to emonCMS
  // /emoncms/input/post.json?node=1&json={power1:100,power2:200,power3:300}
  String url = "/emoncms/input/post.json?node=1&json={dht11_temp:";
  request(emonHost, 80, url + (int)temperature + ",dht11_humid:" + (int)humidity + "}&apikey=" + emonKey);
  
  // DHT11 sampling rate is 1HZ.
  // Wait five seconds before retrying
  delay(5000);
}

bool request(const char * host, int port, String url) {
  if ( !client.connect(host, port) ) {
    return false;
  }
      // Make an HTTP GET request
  client.println("GET " + url + " HTTP/1.1");
  client.print("Host: ");
  client.println(host);
  client.println("Connection: close");
  client.println();
  
  return true;
}

