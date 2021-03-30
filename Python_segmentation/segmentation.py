# -*- coding: utf-8 -*-
"""
Created on Tue Mar 30 14:21:26 2021

@author: Klaudia
"""


import sys
def add_numbers(x,y):
   suma = x + y
   return suma

num1 = int(sys.argv[1])
num2 = int(sys.argv[2])

print(add_numbers(num1, num2))


