#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>
#include <stdint.h>
#include <errno.h>

#include <net/if.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/ioctl.h>

#include <linux/can.h>
#include <linux/can/raw.h>

int s;
int ifindex;

int InitCan(const char *ifname)
{
	struct ifreq ifr;
	struct sockaddr_can addr;
	if((s = socket(PF_CAN, SOCK_RAW, CAN_RAW)) < 0)
		return(-1);
	printf("IFR Name: %s\n", ifname);
	strcpy(ifr.ifr_name, ifname);
	ioctl(s, SIOCGIFINDEX, &ifr);
	printf("IOCTL Errno: %d\n", errno);
	ifindex = ifr.ifr_ifindex;
	addr.can_family = AF_CAN;
	addr.can_ifindex = ifr.ifr_ifindex;
	printf("%s at index %d\n", ifname, ifr.ifr_ifindex);

	if(bind(s, (struct sockaddr *)&addr, sizeof(addr)) < 0)
		return(-2);
	printf("Bind Errno: %d\n", errno);
	return(1);	
}

int Send(int id, uint8_t *payload, uint32_t len)
{
	struct can_frame frame;
	frame.can_id = id;
	int nbytes = 0;
	while(len)
	{
		int size = 8;
		if(len < 8)
			size = len;
		memcpy(frame.data, payload, size);
		frame.can_dlc = size;
		payload += size;
		len -= size;
		nbytes += write(s, &frame, sizeof(struct can_frame));
		printf("Write errno: %d\n", errno);
		
	}
	if(nbytes < 0)
		nbytes = -1;
	return(nbytes);
}

struct can_frame Read()
{
	struct can_frame frame;
	read(s, &frame, sizeof(struct can_frame));
	return(frame);
}
