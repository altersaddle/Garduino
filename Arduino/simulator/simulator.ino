
int firstValue = 1;    			
int secondValue = 10;
int thirdValue = 100;			

void setup() {
  Serial.begin(9600); 				//Set baud rate to 9600 on the Arduino
}

void loop() {
  Serial.print("SINGLE\t1\t");
  Serial.println(firstValue); 		 //print the value to Serial monitor
  
  Serial.print("DOUBLE\t2\t");
  Serial.print(secondValue);
  Serial.print("\t");
  Serial.println(thirdValue);
  
  firstValue += 1;
  secondValue += 10;
  thirdValue += 100;
  
  if (firstValue > 10) {
    firstValue = 1;
  }
  if (secondValue > 100) {
    secondValue = 10;
  }
  if (thirdValue > 1000) {
    thirdValue = 100;
  }
  delay(5000);                        //delay for 25 seconds
}


