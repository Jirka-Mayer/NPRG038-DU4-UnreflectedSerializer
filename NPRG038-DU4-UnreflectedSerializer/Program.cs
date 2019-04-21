using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace UnreflectedSerializer
{
    /// <summary>
    /// Describes, which members of a type to serialize
    /// </summary>
    public class Descriptor
    {
        public List<Member> members = new List<Member>();

        public class Member
        {
            public string name;
            public Func<object, object> getter;
            public Descriptor descriptor;
        }
    }

    /// <summary>
    /// Construction helper for a Descriptor class
    /// </summary>
    /// <typeparam name="T">What type is being described</typeparam>
    public class Descriptor<T> : Descriptor
    {
        public Descriptor<T> With<U>(string memberName, Func<T, U> memberGetter)
        {
            members.Add(new Member() {
                name = memberName,
                getter = (object instance) => memberGetter.Invoke((T)instance),
                descriptor = DescriptorSuite.descriptors[typeof(U)]
            });

            return this;
        }
    }

    /// <summary>
    /// List of all registered descriptors
    /// </summary>
    static class DescriptorSuite
    {
        public static Dictionary<Type, Descriptor> descriptors = new Dictionary<Type, Descriptor>() {
            
            // primitives
            { typeof(string), null },
            { typeof(int), null }

        };
        
        static DescriptorSuite()
        {
            // add all non-primitive types; order matters

            descriptors.Add(
                typeof(Address), new Descriptor<Address>()
                    .With<string>("Street", a => a.Street)
                    .With<string>("City", a => a.City)
            );

            descriptors.Add(
                typeof(Country), new Descriptor<Country>()
                    .With<string>("Name", a => a.Name)
                    .With<int>("AreaCode", a => a.AreaCode)
            );

            descriptors.Add(
                typeof(PhoneNumber), new Descriptor<PhoneNumber>()
                    .With<Country>("Country", a => a.Country)
                    .With<int>("Number", a => a.Number)
            );

            descriptors.Add(
                typeof(Person), new Descriptor<Person>()
                    .With<string>("FirstName", a => a.FirstName)
                    .With<string>("LastName", a => a.LastName)
                    .With<Address>("HomeAddress", a => a.HomeAddress)
                    .With<Address>("WorkAddress", a => a.WorkAddress)
                    .With<Country>("CitizenOf", a => a.CitizenOf)
                    .With<PhoneNumber>("MobilePhone", a => a.MobilePhone)
            );
        }
    }

    /// <summary>
    /// Describes root of a data tree, performs serialization
    /// </summary>
    /// <typeparam name="T">Type of the root node</typeparam>
    public class RootDescriptor<T>
    {
        public void Serialize(TextWriter writer, T instance)
        {
            if (!DescriptorSuite.descriptors.ContainsKey(typeof(T)))
                throw new Exception("Unsupported type: " + typeof(T));

            SerializeViaDescriptor(writer, typeof(T).Name, DescriptorSuite.descriptors[typeof(T)], instance);
        }

        private void SerializeViaDescriptor(TextWriter writer, string tag, Descriptor desc, object instance)
        {
            if (desc == null)
            {
                SerializePrimitive(writer, tag, instance);
                return;
            }

            writer.WriteLine("<" + tag + ">");
            foreach (Descriptor.Member m in desc.members)
                SerializeViaDescriptor(writer, m.name, m.descriptor, m.getter(instance));
            writer.WriteLine("</" + tag + ">");
        }

        private void SerializePrimitive(TextWriter writer, string tag, object primitive)
        {
            if (
                primitive.GetType() == typeof(int) ||
                primitive.GetType() == typeof(string)
            )
            {
                writer.WriteLine(
                    "<" + tag + ">" + primitive.ToString() + "</" + tag + ">"
                );
                return;
            }

            throw new Exception("Unknown primitive: " + primitive.GetType());
        }
    }

    /////////////////////////////////////////////////////////////////////////////
    //                               Boring code                               //
    /////////////////////////////////////////////////////////////////////////////

    class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
    }

    class Country
    {
        public string Name { get; set; }
        public int AreaCode { get; set; }
    }

    class PhoneNumber
    {
        public Country Country { get; set; }
        public int Number { get; set; }
    }

    class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Address HomeAddress { get; set; }
        public Address WorkAddress { get; set; }
        public Country CitizenOf { get; set; }
        public PhoneNumber MobilePhone { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            RootDescriptor<Person> rootDesc = GetPersonDescriptor();

            var czechRepublic = new Country { Name = "Czech Republic", AreaCode = 420 };
            var person = new Person
            {
                FirstName = "Pavel",
                LastName = "Jezek",
                HomeAddress = new Address { Street = "Patkova", City = "Prague" },
                WorkAddress = new Address { Street = "Malostranske namesti", City = "Prague" },
                CitizenOf = czechRepublic,
                MobilePhone = new PhoneNumber { Country = czechRepublic, Number = 123456789 }
            };

            rootDesc.Serialize(Console.Out, person);
        }

        static RootDescriptor<Person> GetPersonDescriptor()
        {
            var rootDesc = new RootDescriptor<Person>();

            return rootDesc;
        }
    }
}
