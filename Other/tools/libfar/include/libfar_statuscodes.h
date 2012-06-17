/*
  This file is a part of libfar and is licensed under the X11 License.
*/

/*
  Status: Need opcode
*/
#define REFPACK_NEEDOPCODE1 0x0100		/* Need 1st byte of opcode */

#define REFPACK_NEEDOPCODE2of2 0x0110	/* Need 2nd byte of 2-byte opcode */

#define REFPACK_NEEDOPCODE2of3 0x0120	/* Need 2nd byte of 3-byte opcode */
#define REFPACK_NEEDOPCODE3of3 0x0121	/* Need 3rd byte of 3-byte opcode */

#define REFPACK_NEEDOPCODE2of4 0x0130	/* Need 2nd byte of 4-byte opcode */
#define REFPACK_NEEDOPCODE3of4 0x0131	/* Need 3rd byte of 4-byte opcode */
#define REFPACK_NEEDOPCODE4of4 0x0132	/* Need 4th byte of 4-byte opcode */

/*
  Status: Need proceeding text
*/
#define REFPACK_NEEDPROCEEDINGDATA 0x0200
#define REFPACK_NEEDREFERENCEDDATA 0x0300