﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace EngineSimulator
{
    //http://www.asawicki.info/Mirror/Car%20Physics%20for%20Games/Car%20Physics%20for%20Games.html <- can be usefull

    //http://www.carfolio.com/specifications/models/car/?car=140591 <- more spec but for different model

    // http://carsmind.com/specification.php?make=TOYOTA&model=Yaris - technical spec
    /*
        FOR NOW LET'S TAKE THIS ONE:
        
        Technical specification TOYOTA Yaris 1.3 2008Make:	TOYOTA
        Model:	Yaris 1.3
        Year:	2008
        Car category:	Small / Economy Cars
        Car engine position:	Front
        Car engine:	1298 ccm (78,80 cubic inches)
        Car engine type:	in-line, 4-cyl
        Car valves per cylinder:	4
        Car max power:	87.00 PS (63,68 kW or 85,57 HP) at 6000 Rev. per min.
        Car max torque:	122.00 Nm (12,34 kgf-m or 89,55 ft.lbs) at 4200 Rev. per min.
        Car bore stroke:	72.0 x 79.7 mm (2,79 x 3.1 inches)
        Car compression:	10.0:1
        Car top speed:	175.0 km/h (108,16 mph)
        Car fuel:	Gasoline, unleaded 95
        Car transmission:	Manual, 5-speed
        Car power per weight:	0.0888 PS/kg
        0 100 km h 0 62 mph:	12.1 seconds
        Car dr
        Car seats:	5
        Car passenger space:	2662 litres (699,49 gallons)
        Car doors:	3
        Car front tire:	175/65-R14
        Car rear tire:	175/65-R14
        Car chassis:	Hatchback
        Car co2 emissions:	165.0 g/km
        Car turn circle:	10 m (383,87 inches)
        Car weight:	984 kg (2158,45 pounds)
        Car towing weight:	906 kg (1987,41 pounds)
        Car total length:	3650 mm (142,98 inches)
        Car total width:	1670 mm (65,37 inches)
        Car total height:	1510 mm (59,10 inches)
        Car max weight with load:	1368 kg (3000,82 pounds)
        Car wheelbase:	2380 mm (93,23 inches)
        Car cooling:	Liquid
        Car front brakes type:	Ventilated disks
        Car rear brakes type:	Disks
        Car cargo space:	205 litres (53,73 gallons)
        Car lubrication:	Wet sump
        Car aerodynamic dragcoefisient:	1
        Car fuel with mixed drive:	5.8 litres/100 km (40,36 miles per gallon)
        Car fuel tank capacity:	45.0 litres (11,83 gallons)
    */

    /* 
        TORQUE AND POWER DIAGRAM:
        
        http://rototest-research.eu/popup/performancegraphs.php?ChartsID=582
      
        also avitable in .pdf file in projects dir 
     */


    /*
     * GEARS:
     * http://en.wikipedia.org/wiki/Toyota_C_transmission
     * 
     * z wiki:
     * 1st	    2nd	     3rd	4th	     5th    Reverse	 Final
     * 3.545	1.913	1.310	1.027	0.850	3.214	 3.550
     * 
     * odwrotnosc (1/x) tego z wiki:
     * 1 -> 0,28208744710860366713681241184767
     * 2 -> 0,52273915316257187663355985363304
     * 3 -> 0,76335877862595419847328244274809
     * 4 -> 0,97370983446932814021421616358325
     * 5 -> 1,1764705882352941176470588235294
     * 
     * Final =  Differential Ratio  = 1 / 3.550 = 0,28169014084507042253521126760563
     */

    public class EnginePointStats
    {
        public double RPM; // 1/s
        public double power; // W //engine output power (on wheel - when there are no transmission)
        public double torque; // N*m //ending output torque (on wheel - when there are no transmission)

        public EnginePointStats(double _RPM, double _power, double _torque)
        {
            RPM = _RPM;
            power = _power;
            torque = _torque;
        }
    }

    abstract class CarModel
    {
        // Gravitational Acceleration at the Earths surface. 
        public const double EARTH_GRAV_CONST = 9.8067;

        public abstract List<EnginePointStats> EngineStats { get; }
        public abstract double StaticEngineResistanceForces { get;  } //N*m
        public abstract double DynamicEngineResistancePerRPM { get; } //N*m/RPM
        public abstract double EngineMomentum { get; } //kg * m^2
        public abstract double Torque { get; }
        public abstract double Power { get; }
        public abstract double WheelRadius { get; }
        public abstract double DifferentialRatio { get; }
        public abstract double MaxEngineRPM { get; }
        public abstract double Mass { get; }
        public abstract double Width{ get; }
        public abstract double Height{ get; }
        public abstract double[] GearTransmissionRatios { get; }
        public abstract int MaxGear { get; }
        public abstract double WheelMomentum { get; }
        public abstract double WheelMass { get; }
        public abstract double WheelsNo { get; }
        public double ExternalResistanceForces { get; set; }
        public double RPM { get; set; }
        public double BrakingLevel { get; set; } // in range [0,1]
        public abstract double MaxBreakingForcePerWheel { get; }
        public abstract int BrakingWheelsNo { get; }
        public double DistanceDoneInMeters { get; set; }
        public abstract int AcceleratingWheelsNo { get; }
        public double StaticFrictionFactor { get; set; } //set coz it van vary when evironment changes
        public double KineticFrictionFactor { get; set; } //set coz it van vary when evironment changes

        //air resistance part
        public double AirDensity { get { return 1.2; } } //kg/m^2 //source: http://pl.wikipedia.org/wiki/Gęstość_powietrza
        public abstract double CarDragCoeffcient { get; }
        public double CarFrontSurface { get { return Height * Width * 0.85; } } //TODO: pod samochodem jest spora szczelina, ktora trzeba uwzglednic (na razie dalem na pale *0.85)
        public double AerodynemicResistance { get { return CarDragCoeffcient * AirDensity * Math.Pow(SpeedInMetersPerSecond, 2.0) * CarFrontSurface / 2; } }

        //rolling resistance //from: http://www.engineeringtoolbox.com/rolling-friction-resistance-d_1303.html //TODO: make it some better way
        public abstract double TyrePresure { get; }
        public double RollingResistanceCoefficient { get { return 0.005 + 1.0 / TyrePresure * (0.01 + 0.0095 * Math.Pow(SpeedInKilometersPerHour / 100.0, 2.0)); } }
        public double RollingResistance { get { return RollingResistanceCoefficient * Mass * EARTH_GRAV_CONST; } }

        private double __THROTTLE_OPPENING_LEVEL__ = 0.0;
        public double ThrottleOppeningLevel
        {
            get { return __THROTTLE_OPPENING_LEVEL__; }
            set
            {
                if (value < 0.0 || value > 1.0)
                    throw new ArgumentException("throttle oppening level is out of [0,1] range");

                __THROTTLE_OPPENING_LEVEL__ = value;
            }
        }

        public double RPS { get { return RPM / 60; } } //obroty na sekunde
        public double SpeedInMetersPerSecond { get { return RPS * TransmissionRate * WheelCircuit; } }
        public double SpeedInKilometersPerHour { get { return SpeedInMetersPerSecond * 3.6; } }
        public double ForwardForceOnWheelsFromEngine { get { return Torque / TransmissionRate / WheelRadius * ThrottleOppeningLevel; } }
        public double WheelCircuit { get { return WheelRadius * 2 * Math.PI; } }
        public double TransmissionRate { get { return GearRatio(CurrGear) * DifferentialRatio; } }
        public double DynamicEngineResistanceForces { get { return DynamicEngineResistancePerRPM * RPM; } }
        public double EngineResistanceForces { get { return DynamicEngineResistanceForces + StaticEngineResistanceForces; } }
        public double EngineResistanceForcesOnWheels { get { return EngineResistanceForces / TransmissionRate / WheelRadius; } }
        public double BrakingForce { get { return BrakingLevel * BrakingWheelsNo * MaxBreakingForcePerWheel; } }

        private int __CURR_GEAR__ = 1;
        public int CurrGear
        {
            get { return __CURR_GEAR__; }
            
            /*
             *   I assume that total momnentum is unchanged:
             *   RPS * 
             *   (M_engine +                                    // engine momentum 
             *   transmission_rate * M_wheel * no_of_wheels +   // wheels momentum
             *   transmission_rate * wheel_circuit * car_mass)  // car mass momentum
             *       = const;
             */
            set
            {
                if (value != __CURR_GEAR__)
                {
                    double oldTransmissionRate = TransmissionRate;
                    __CURR_GEAR__ = value;
                    double newTranmissionRate = TransmissionRate;

                    RPM = RPM *
                        (EngineMomentum +
                        oldTransmissionRate * WheelMomentum * WheelsNo +
                        oldTransmissionRate * WheelCircuit * Mass)
                        /
                        (EngineMomentum +
                        newTranmissionRate * WheelMomentum * WheelsNo +
                        newTranmissionRate * WheelCircuit * Mass);
                }
            }
        }

        public double GearRatio(int gear)
        {
            if (gear > MaxGear || gear < 1)
                throw new ArgumentException("gear is invalid");

            return GearTransmissionRatios[gear - 1];
        }

        public CarModel()
        {
            RPM = 0;
            ExternalResistanceForces = 0;
            ThrottleOppeningLevel = 0;
            CurrGear = 1;
            DistanceDoneInMeters = 0;
        }

        public abstract void Start();

        private static double LinearApprox(double x1, double y1, double x2, double y2, double wantedX)
        {
            double yDiff = y2 - y1;
            double xDiff = x2 - x1;
            double changePerPoint = yDiff / xDiff;
            double wantedXdiffFromX1 = wantedX - x1;

            return y1 + changePerPoint * wantedXdiffFromX1;
        }

        private bool areEngineStatsSortedByRPM = false;
        protected double GetTorque(double RPM)
        {
            if (!areEngineStatsSortedByRPM)
            {
                EngineStats.OrderBy(stat => stat.RPM);
                areEngineStatsSortedByRPM = true;
            }

            double torque;
            var engineStat = EngineStats.Find(x => x.RPM == RPM);
            if (engineStat != null) //RPM is a point on our map
            {
                torque = engineStat.torque;
            }
            else
            {
                //RPM is not a point on our map (and it has top be approximated)
                if (RPM > EngineStats.First().RPM && RPM < EngineStats.Last().RPM) //it is in scale
                {
                    var p1 = EngineStats.Find(x => x.RPM > RPM);
                    var p2 = EngineStats.FindLast(x => x.RPM < RPM); //can be optimized
                    torque = LinearApprox(p1.RPM, p1.torque, 0, 0, RPM);
                }
                else if (RPM < EngineStats.First().RPM) //if its under a scale
                {
                    var p1 = EngineStats[0];
                    torque = LinearApprox(p1.RPM, p1.torque, 0, 0, RPM);
                }
                else //if its over a scale
                {
                    var p1 = EngineStats.Last();
                    torque = LinearApprox(p1.RPM, p1.torque, MaxEngineRPM, 0, RPM);
                }
            }

            return torque;
        }
        /* MY CALCULATIONS ABOUT CAR DYNAMICS (TORQUE BASED):
         * torque_current = E_engine * M_engine + //E = epsilon, M = momentum //engine ineria
         *          + E_wheels * M_wheels +  //wheels inertia
         *          + a_car * m_car / transmission_rate / r_wheel + //a = acceleration, m = mass, r = radius //car dynamics //ERROR - it probably should be multiplied by transmission //TODO: look for mistake
         *          + internal_forces //???
         *          + external_forces / transmission_rate / r_wheel //external forces on wheel
         *
         * transmission_rate = omega_wheel / omega_engine 
         * so:
         * E_wheels = E_engine * transmission_rate  
         * 
         * 
         *  torque_current = torque[curr_rpm] * gas_in_peccents_current
         */
        private DateTime lastTickTime = DateTime.Now;
        public void CalculationsTick()
        {
            TimeSpan timeFromLastTick = DateTime.Now - lastTickTime;
            lastTickTime = DateTime.Now;
            double tyresForceBallance = 0;

            tyresForceBallance += ForwardForceOnWheelsFromEngine;
            tyresForceBallance -= EngineResistanceForcesOnWheels;
            tyresForceBallance -= RollingResistance;
            tyresForceBallance -= BrakingForce * Math.Sign(SpeedInMetersPerSecond); //force opposite to speed vector

            double carForceBallance = 0;

            int workingWheels;
            if (BrakingForce * Math.Sign(SpeedInMetersPerSecond) > ForwardForceOnWheelsFromEngine - EngineResistanceForcesOnWheels - RollingResistance)
            {
                workingWheels = BrakingWheelsNo;
            }
            else
            {
                workingWheels = AcceleratingWheelsNo;
            }

            if (Math.Abs(tyresForceBallance) < Mass * EARTH_GRAV_CONST * StaticFrictionFactor * workingWheels / WheelsNo)
            {
                carForceBallance = tyresForceBallance;
            }
            else
            {
                carForceBallance = Math.Sign(tyresForceBallance) * Mass * EARTH_GRAV_CONST * KineticFrictionFactor * workingWheels / WheelsNo; 
                /*
                 * IMPORTANT: NOTE: 
                 * this model just wastes energy of slide
                 * 
                 * in real this energy forces wheels to move wheels and engine faster, but not accelerate the car
                 */

                //TODO: fix it by adding some variables for connections (engine <---> wheels) and (wheels <---> enviroment)
                Console.Write("IM SLIDING ");
            }

            carForceBallance -= AerodynemicResistance;
            carForceBallance -= ExternalResistanceForces;

            double Acceleration = //a = F/m (but we got some additional radial inetrions, so we have to remember about E = M / I)
                carForceBallance /
                    (Mass +
                    EngineMomentum / TransmissionRate / WheelRadius + // engine inertion
                    WheelsNo * WheelMomentum / WheelRadius); // wheels inertion

            double Epsilon_engine = Acceleration / WheelCircuit / TransmissionRate;

            RPM += Epsilon_engine * timeFromLastTick.TotalSeconds * 60.0;

            if (RPM < 5)
            {
                RPM = 0;
            }

            DistanceDoneInMeters += Math.Abs(SpeedInMetersPerSecond) * timeFromLastTick.TotalSeconds;
        }
    }

    class ToyotaYaris : CarModel
    {
        private List<EnginePointStats> __ENGINE_STATS__ = new List<EnginePointStats>()     
        {
            new EnginePointStats(1493, 13500, 86.1),
            new EnginePointStats(2010, 20200, 96.1),
            new EnginePointStats(2508, 27000, 102.8),
            new EnginePointStats(3010, 33200, 105.2),
            new EnginePointStats(3508, 41400, 112.6),
            new EnginePointStats(4017, 47500, 112.8),
            new EnginePointStats(4209, 50200, 113.8),
            new EnginePointStats(4416, 52800, 114.3),
            new EnginePointStats(4616, 54800, 113.6),
            new EnginePointStats(5011, 55800, 106.3),
            new EnginePointStats(5512, 57500, 99.6),
            new EnginePointStats(5811, 57300, 94.2),
            new EnginePointStats(6006, 56200, 89.4),
            new EnginePointStats(6203, 56100, 86.4)
        };
        public override List<EnginePointStats> EngineStats { get { return __ENGINE_STATS__; } }
        private double[]  __GEAR_TRANMISSIONS_RATIOS__ = new double[]{
                0.2820874, // gear 1
                0.5227392, // 2
                0.7633588, // 3
                0.9737099, // 4
                1.1764705  // 5
        };
        public override double[] GearTransmissionRatios { get { return __GEAR_TRANMISSIONS_RATIOS__; } }

        public override double DifferentialRatio { get { return 1.0/3.550; } }
        public override int MaxGear { get { return 5; } }
        public override double StaticEngineResistanceForces{get { return 10.0; }}
        public override double DynamicEngineResistancePerRPM { get { return 0.0009; } }
        public override double EngineMomentum { get { return 8.0; } } //TODO: its actually random value
        public override double Torque { get { return this.GetTorque(RPM); } }
        public override double Power { get { throw new NotImplementedException(); } } //NOTE: I think power is not needed to do anything in a car
        public override double WheelRadius { get { return 14.0 * 2.54 / 2 / 100 + 0.65 * 0.175; } } // = 0,29155m //in meters // wheel: 175/65-R14
        public override double MaxEngineRPM { get { return 7000.0; } }
        public override double Mass { get { return 984.0; } }
        public override double WheelMomentum { get { return 1.0 / 2.0 * WheelMass * WheelRadius * WheelRadius; } } // 1/2 M * R^2 for cylinder //TODO: calculate it better
        public override double WheelsNo { get { return 4.0; } }
        public override double WheelMass { get { return 6.5 + 6.5; } } //TODO: find true data //mass of wheel and tire
        public override double CarDragCoeffcient { get { return 0.29; } } //from: http://en.wikipedia.org/wiki/Automobile_drag_coefficient
        public override double Width { get { return 1.66; } } //from: http://en.wikipedia.org/wiki/Toyota_Vitz
        public override double Height { get { return 1.51; } }
        public override double TyrePresure { get { return 1.7; } } //TODO: CHECK IT!!! 
        public override double MaxBreakingForcePerWheel { get { return 10000.0; } }  //TODO: its complately random value, but seems legit (excluding sliding)
        public override int BrakingWheelsNo { get { return 2; } } //only front wheels are breaking
        public override int AcceleratingWheelsNo { get { return 2; } }

        public ToyotaYaris()
        {
            //tarcie guma-asfalt bazujac na SLABYCH zrodlach z neta //TODO: find some real data
            StaticFrictionFactor = 0.9; 
            KineticFrictionFactor = 0.6; 
        }

        public override void Start()
        {
            RPM = 1000;
        }
    }

    class EngineSimulator
    {
        public CarModel model;

        const double SIMULATION_TIMER_INTERVAL_IN_MS = 10.0;
        Timer SimulationTimer = new Timer(SIMULATION_TIMER_INTERVAL_IN_MS);
        
        public EngineSimulator(CarModel _model)
        {
            model = _model;

            SimulationTimer.Elapsed += SimulationTimer_Elapsed;
            SimulationTimer.Start();
        }

        void SimulationTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            model.CalculationsTick();
        }
    }
}
