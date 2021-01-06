.data
	one		DD	3f800000h
	zero	DD	00000000h
	temp	DD	00000000h
.code	

GetDistances256ASM PROC						;
	MOV		R15,	[RSP + 28h]				;
	VBROADCASTSS	ymm3, xmm3				;
	MOVQ	xmm4,	R15						;
	VBROADCASTSS	ymm4, xmm4				;
	VSUBPS	ymm5,	ymm3, [RDX]				;
	VSUBPS	ymm6,	ymm4, [R8]				;
	VMULPS	ymm7,	ymm5, ymm5				;
	VMULPS	ymm8,	ymm6, ymm6				;
	VADDPS	ymm9,	ymm7, ymm8				;
	VSQRTPS	ymm10,	ymm9					;
	VMOVUPS	[RCX],	ymm10					;
	RET										;
GetDistances256ASM ENDP						;

GetDistancesASM PROC

	VBROADCASTSS	ymm15,	xmm3
	MOV		R13,	[RSP + 28h]
	MOVQ	xmm14,	R13
	VBROADCASTSS	ymm14,	xmm14
	MOV		R15,	[RSP + 30h]
	SHL		R15,	5
	MOV		R14,	0
	POINTERLOOP:
			VSUBPS	ymm13,	ymm15,	[RCX + R14]
			VSUBPS	ymm12,	ymm14,	[RDX + R14]
			VMULPS	ymm11,	ymm13,	ymm13
			VMULPS	ymm10,	ymm12,	ymm12
			VADDPS	ymm9,	ymm11,	ymm10
			VSQRTPS	ymm8,	ymm9
			VMOVUPS	[R8 + R14],		ymm8
			ADD		R14D,	32
			CMP		R14D,	R15D
			JL		POINTERLOOP
	RET
GetDistancesASM ENDP

ClearTrails256ASM PROC
	CMP		R8,		0
	JLE		ENDING
	SHL		R8,		5
	VBROADCASTSS	ymm1, xmm1
	MOV		R15,	0
	POINTERLOOP:
			VMOVUPS	[RCX + R15],	ymm1
			ADD		R15,	32
			CMP		R15D,	R8D
			JL		POINTERLOOP
	ENDING:
	RET
ClearTrails256ASM ENDP

CalculateCityNumberASM PROC
	VZEROALL							;Zero all the ymm registers
	MOV		R15,	[RSP + 28h]			;R15	<-	alpha
	MOV		R14,	[RSP + 30h]			;R14	<-	beta
	MOV		R13,	[RSP + 38h]			;R13	<-	pointercount
	SHL		R13,	5					;32 bytes
	MOV		R12D,	0					;R12D	<-	0						loop counter
	;Fly through all the stacked pointers
	POINTERLOOP:		
			VMOVUPS	ymm12,	[RDX + R12]				;ymm12	<-	ymm14						Get a copy of trailsColumn
			VMOVUPS ymm11,	[R8  + R12]				;ymm11	<-	ymm13						Get a copy of graphColumn
			CMP		R15D,	1
			JLE		TRAILPOWER1
			;trailsColumn[i]^alpha
			MOV		R11D,	1						;R11D	<-	1							Perform the loop
			TRAILPOWER:								
					VMULPS	ymm12,	ymm12,	[RDX + R12]	;Multiply trailsColumn[i]^R11D with trailsColumn[i]
					INC		R11D					;R11D++
					CMP		R11D,	R15D			;R11D	<?	R15D
					JL		TRAILPOWER				;JUMP	->	TRAILPOWER		
			TRAILPOWER1:
			CMP		R14D,	1
			JLE		GRAPHPOWER1
			;graphColumn[i]^beta
			MOV		R11D,	1						;R11D	<-	1							Perform the loop
			GRAPHPOWER:								
					VMULPS	ymm11,	ymm11,	[R8 + R12]	;Multiply graphColumn[i]^R11D with graphColumn[i]
					INC		R11D					;R11D++
					CMP		R11D,	R14D			;R11D	<?	R14D
					JL		GRAPHPOWER				;JUMP	->	GRAPHPOWER
			GRAPHPOWER1:
			VDIVPS	ymm10,	ymm12,	ymm11		;Divide trailsColumn[i]^alpha over graphColumn[i]^beta
			VMULPS	ymm9,	ymm10, 	[RCX + R12]	;Multiply pheromone[i] with visited		(yes => 0, no => 1)
			VMOVUPS	[R9 + R12],		ymm9		;[R10]	<-	numerators[i]
			VADDPS	ymm8,	ymm8,	ymm9		;Increase total pheromone level
			ADD		R12D,	32					;R12D	<-	32							Increase loop counter
			CMP		R12D,	R13D				;R12D	<?	pointerCount
			JL		POINTERLOOP					;JUMP	->	POINTERLOOP
	VEXTRACTF128	xmm7,	ymm8,	1	;Extract upper part of ymm register
	VADDPS	xmm6,	xmm7,	xmm8		;Add lower and upper part of the register
	VPSRLDQ	xmm5,	xmm6,	8			;In second register, shift right 8B
	VADDPS	xmm4,	xmm5,	xmm6		;Add lower and upper halfs of xmm register
	VPSRLDQ	xmm3,	xmm4,	4			;In another register, shift right 4B
	VADDPS	xmm2,	xmm3,	xmm4		;Add remaining values
	VBROADCASTSS	ymm2,	xmm2		;Set ymm register to pheromone values
	VBROADCASTSS	ymm1,	one			;Set ymm register to value 1
	VDIVPS	ymm14,	ymm1,	ymm2		;Get 1 / pheromone
	MOV		R11D,	[RSP + 40h]			;Random
	MOV		temp,	R11D				;temp	<-	R11D					Load random value to the variable
	FLD		temp						;ST(0)	<-	temp					Load value to float stack
	FLDZ								;ST(0)	->	ST(1)	ST(0)	<-	0f	Load zero to the float stack
	MOV		R12D,	0					;R12D	<-	0				
	PROBABILITIESLOOP:
			VMULPS	ymm9,	ymm14,	[R9 + R12]
			VEXTRACTF128	xmm7,	ymm9,	1
			VEXTRACTPS		temp,	xmm9,	0	;1 number
			FLD		temp
			FADDP	st(1),	st
			FCOM	
			FSTSW	AX
			FWAIT
			SAHF
			JA		RETURN
			JZ		RETURN
			ADD		R12D,	4	
			VEXTRACTPS		temp,	xmm9,	1	;2 number
			FLD		temp
			FADDP	st(1),	st
			FCOM
			FSTSW	AX
			FWAIT
			SAHF
			JA		RETURN
			JZ		RETURN
			ADD		R12D,	4	
			VEXTRACTPS		temp,	xmm9,	2	;3 number
			FLD		temp
			FADDP	st(1),	st
			FCOM
			FSTSW	AX
			FWAIT
			SAHF
			JA		RETURN
			JZ		RETURN
			ADD		R12D,	4	
			VEXTRACTPS		temp,	xmm9,	3	;4 number
			FLD		temp
			FADDP	st(1),	st
			FCOM
			FSTSW	AX
			FWAIT
			SAHF
			JA		RETURN
			JZ		RETURN
			ADD		R12D,	4	
			VEXTRACTPS		temp,	xmm7,	0	;5 number
			FLD		temp
			FADDP	st(1),	st
			FCOM
			FSTSW	AX
			FWAIT
			SAHF
			JA		RETURN
			JZ		RETURN
			ADD		R12D,	4	
			VEXTRACTPS		temp,	xmm7,	1	;6 number
			FLD		temp
			FADDP	st(1),	st
			FCOM
			FSTSW	AX
			FWAIT
			SAHF
			JA		RETURN
			JZ		RETURN
			ADD		R12D,	4	
			VEXTRACTPS		temp,	xmm7,	2	;7 number
			FLD		temp
			FADDP	st(1),	st
			FCOM
			FSTSW	AX
			FWAIT
			SAHF
			JA		RETURN
			JZ		RETURN
			ADD		R12D,	4	
			VEXTRACTPS		temp,	xmm7,	3	;8 number
			FLD		temp
			FADDP	st(1),	st
			FCOM
			FSTSW	AX
			FWAIT
			SAHF
			JA		RETURN
			JZ		RETURN
			ADD		R12D,	4	
			CMP		R12D,	R13D
			JL		PROBABILITIESLOOP
	RETURN:
			FCOMP
			FCOMP
			SHR		R12D,	2
			MOV		EAX,	R12D
	ENDING:
	RET
CalculateCityNumberASM ENDP


UpdateEvaporationASM PROC
	CMP		R8,		0
	JLE		ENDING
	SHL		R8,		5
	MOV		R15,	0
	POINTERLOOP:
			VBROADCASTSS	ymm1,	xmm1
			VMULPS	ymm2,	ymm1,	[RCX + R15]
			VMOVUPS	[RCX + R15],	ymm2
			ADD		R15,	32
			CMP		R15D,	R8D
			JL		POINTERLOOP
	ENDING:
	RET	
UpdateEvaporationASM ENDP

AddContributionsToTrails PROC

CMP		R8,		0
JLE		ENDING
SHL		R8,		5
MOV		R15,	0
ADDINGLOOP:
		VMOVUPS	ymm0,	[RCX + R15]
		VADDPS	ymm1,	ymm0,	[RDX + R15]
		VMOVUPS	[RDX + R15],	ymm1
		ADD		R15,	32
		CMP		R15,	R8
		JL		ADDINGLOOP
ENDING:
RET

AddContributionsToTrails ENDP

UpdateTrails PROC
	SHL		R9,		5
	MOV		R15,	0
	UPDATELOOP:
			VMOVUPS	ymm0,	[RCX + R15]
			VBROADCASTSS	ymm2,	xmm2
			VMULPS	ymm0,	ymm0,	ymm2
			VADDPS	ymm0,	ymm0,	[RDX + R15]
			VMOVUPS	[RCX + R15],	ymm0
			ADD		R15,	32
			CMP		R15,	R9
			JL		UPDATELOOP
	RET
UpdateTrails ENDP


ChangeFloat PROC
	SHL		RDX,	2
	VMOVD	R15D,	xmm2
	MOV		[RCX + RDX],	R15D
	RET
ChangeFloat ENDP

END