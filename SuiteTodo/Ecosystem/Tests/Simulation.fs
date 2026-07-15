[<AutoOpen>]
module Simulation

[<assembly: LibLifeCycleTest.TestRunner.SimulationTestFramework>]
do ()

open System.Reflection
open LibLifeCycleTest

let private createdTestTodoEcosystem =
    SuiteTodo.LifeCycles.AllLifeCycles.createTodoEcosystem ()

let todoLifeCycle = createdTestTodoEcosystem.TodoLifeCycle

let private simulation_without_initializer =
    SimulationBuilder(createdTestTodoEcosystem.Ecosystem, Assembly.GetExecutingAssembly())

let initializer =
    simulation_without_initializer {
        return ()
    }

let simulation =
    simulation_without_initializer.WithInitializer initializer
