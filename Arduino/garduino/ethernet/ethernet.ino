#include <UIPEthernet.h>
#include <SimpleDHT.h>


// for DHT11, 
//      VCC: 5V or 3V
//      GND: GND
//      DATA: 2
int pinDHT11 = 2;
SimpleDHT11 dht11;

const char* emonHost = "emonhost"; // update to suit your environment
const char* emonKey = "emonkey";   // use write keys from emoncms

const static uint8_t myip[] = {192,168,1,111};
const static uint8_t mygw[] = {192,168,1,1};
//const static uint8_t mymask[] = {255,255,255,0};
const static uint8_t mydns[] = {192,168,1,1};

// Ha ha yes!  We can invent our own MAC
static uint8_t mac[] = {0x74,0x69,0x69,0x2D,0x30,0x31};

EthernetClient client;
signed long next;
signed long wait;

void setup() {
  Serial.begin(115200);
  Serial.println("Ethernet based temperature monitor");

  Ethernet.begin(mac, myip, mydns, mygw);

  Serial.print(F("localIP: "));
  Serial.println(Ethernet.localIP());
  Serial.print(F("subnetMask: "));
  Serial.println(Ethernet.subnetMask());
  Serial.print(F("gatewayIP: "));
  Serial.println(Ethernet.gatewayIP());
  Serial.print(F("dnsServerIP: "));
  Serial.println(Ethernet.dnsServerIP());

  wait = (30 * 60000) - 500;
}

void loop() {
  Serial.print(F("Sample DHT11..."));
  
  // read without samples.
  byte temperature = 0;
  byte humidity = 0;
  if (dht11.read(pinDHT11, &temperature, &humidity, NULL)) {
    Serial.println(F("failed."));
    delay(5000);
    return;
  }
  
  Serial.print(F("OK: "));
  Serial.print((int)temperature); Serial.print(F(" C, ")); 
  Serial.print((int)humidity); Serial.println(F(" %"));

  // Upload to emonCMS
  // /emoncms/input/post.json?node=1&json={power1:100,power2:200,power3:300}
  String args = String(F("?node=2&json=%7Bdht11_temp:")) + (int)temperature + F(",dht11_humid:") + (int)humidity + F("%7D&apikey=");
  request(emonHost, "/emoncms/input/post.json", args, emonKey );
  Ethernet.maintain();
    
  // DHT11 sampling rate is 1HZ.
  // Wait 5 seconds before retrying
  delay(wait);
}

void request(const char * host, char * path, String args, const char * key) {
  next = millis() + 5000;
  if (client.connect(emonHost, 80)) {
    client.print(F("GET "));
    client.print(path);
    client.print(args);
    client.print(key);
    client.print(F(" HTTP/1.0\r\n"));
    Ethernet.maintain();
    client.print(F("Host: "));
    client.print(host);
    client.print(F("\r\nConnection: close\r\n\r\n"));
    while(client.available()==0)
    {
      if (next - millis() < 0)
        goto close;
    }
    int size;
   /* while((size = client.available()) > 0)
    {
      uint8_t* msg = (uint8_t*)malloc(size);
      size = client.read(msg,size);
      Serial.write(msg,size);
      free(msg);
    }*/
    close:
          //disconnect client
          Serial.println(F("sent message"));
          //client.stop();
  }
  else {
     Serial.println(F("connect failed"));
  }
}

