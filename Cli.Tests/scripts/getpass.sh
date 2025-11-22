#!/bin/sh

if [[ $# -gt 0 ]] ;
then
	if [[ $1 == Username* ]] ;
	then
		echo taut
	fi
	if [[ $1 == Password* ]] ;
	then
		echo Hello!
	fi
fi
